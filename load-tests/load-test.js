/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { parseHTML } from 'k6/html';
import { SharedArray } from 'k6/data';

// =============================================================================
// Configuration
// =============================================================================

const BASE_URL = __ENV.TARGET_URL || 'http://traefik:80';
const TEST_EMAIL = __ENV.TEST_EMAIL || 'user@stickerlandia.com';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || 'Stickerlandia2025!';

// Registration test users get unique emails per VU/iteration
const REG_EMAIL_PREFIX = __ENV.REG_EMAIL_PREFIX || 'loadtest';
const REG_EMAIL_DOMAIN = __ENV.REG_EMAIL_DOMAIN || 'loadtest.stickerlandia.com';

// External URL that gets rewritten to internal Docker network URL
// OAuth redirects use the external hostname which isn't reachable from inside Docker
const EXTERNAL_URL = __ENV.EXTERNAL_URL || 'http://localhost:8080';

// =============================================================================
// Multi-User Pool Support (for GameDay load testing)
// =============================================================================

// Load user pool for GameDay mode - SharedArray is memory-efficient across VUs
let users = [];
try {
  users = new SharedArray('users', function () {
    return JSON.parse(open('./data/users.json')).users;
  });
} catch (e) {
  // users.json doesn't exist - will use single user fallback
}

/**
 * Get credentials for current VU (round-robin assignment).
 * Only uses user pool for GameDay workloads; otherwise uses default TEST_EMAIL/TEST_PASSWORD.
 */
function getCredentialsForVU() {
  const isGameDay = workload.startsWith('gameday:');
  if (!isGameDay || users.length === 0) {
    return { email: TEST_EMAIL, password: TEST_PASSWORD };
  }
  return users[(__VU - 1) % users.length];
}

/**
 * Rewrite external URLs to internal Docker network URLs.
 * OAuth redirects use the configured external hostname (localhost:8080)
 * but k6 inside Docker needs to use the internal hostname (traefik:80).
 */
function rewriteUrl(url) {
  if (url && url.startsWith(EXTERNAL_URL)) {
    return url.replace(EXTERNAL_URL, BASE_URL);
  }
  return url;
}

/**
 * Extract origin (protocol + host) from a URL string.
 * k6 doesn't have the native URL constructor.
 */
function getUrlOrigin(url) {
  const match = url.match(/^(https?:\/\/[^\/]+)/);
  return match ? match[1] : '';
}

/**
 * Get the directory path from a URL (everything up to the last /).
 */
function getUrlDirectory(url) {
  const origin = getUrlOrigin(url);
  const path = url.slice(origin.length);
  const lastSlash = path.lastIndexOf('/');
  return origin + (lastSlash >= 0 ? path.slice(0, lastSlash + 1) : '/');
}

/**
 * Resolve a relative URL against a base URL.
 */
function resolveUrl(baseUrl, relativeUrl) {
  if (relativeUrl.startsWith('http')) {
    return relativeUrl;
  }
  if (relativeUrl.startsWith('/')) {
    return getUrlOrigin(baseUrl) + relativeUrl;
  }
  return getUrlDirectory(baseUrl) + relativeUrl;
}

// Workload configurations
const WORKLOADS = {
  smoke: { vus: 2, duration: '30s' },
  load: {
    stages: [
      { duration: '1m', target: 10 },
      { duration: '3m', target: 30 },
      { duration: '5m', target: 30 },
      { duration: '1m', target: 0 },
    ],
  },
  // GameDay profiles - hardcoded for simplicity
  // Note: Auth RPS is limited by IdP capacity (~10 RPS sustainable)
  'gameday:auth': {
    // Auth load - stress test login/logout at sustainable rate
    scenarios: {
      auth_stress: {
        executor: 'constant-arrival-rate',
        rate: 10,
        timeUnit: '1s',
        duration: '5m',
        preAllocatedVUs: 20,
        maxVUs: 40,
        exec: 'authenticatedFlow',
      },
    },
  },
  'gameday:catalogue': {
    // Heavy catalogue browsing - stress test sticker-catalogue service
    scenarios: {
      catalogue_stress: {
        executor: 'constant-arrival-rate',
        rate: 100,
        timeUnit: '1s',
        duration: '5m',
        preAllocatedVUs: 100,
        maxVUs: 150,
        exec: 'publicBrowsingFlow',
      },
    },
  },
  'gameday:sustained': {
    // Sustained load across all services
    scenarios: {
      auth_flow: {
        executor: 'constant-arrival-rate',
        rate: 10,
        timeUnit: '1s',
        duration: '10m',
        preAllocatedVUs: 20,
        maxVUs: 40,
        exec: 'authenticatedFlow',
      },
      browse_flow: {
        executor: 'constant-arrival-rate',
        rate: 50,
        timeUnit: '1s',
        duration: '10m',
        preAllocatedVUs: 50,
        maxVUs: 80,
        exec: 'publicBrowsingFlow',
      },
      registration_flow: {
        // 2-3 new user registrations per minute
        executor: 'constant-arrival-rate',
        rate: 3,
        timeUnit: '1m',
        duration: '10m',
        preAllocatedVUs: 2,
        maxVUs: 5,
        exec: 'registrationFlow',
      },
    },
  },
};

const workload = __ENV.WORKLOAD || 'smoke';
const scenario = __ENV.SCENARIO || 'mixed'; // 'public', 'auth', 'register', or 'mixed'

export const options = {
  ...WORKLOADS[workload],
  thresholds: {
    http_req_failed: ['rate<0.01'],       // <1% errors
    http_req_duration: ['p(95)<1000'],    // 95th percentile < 1s (auth flows are slower)
    checks: ['rate>0.90'],                // >90% checks pass
  },
};

// =============================================================================
// OAuth Authentication Helper
// =============================================================================

/**
 * Performs the full OAuth 2.1 Authorization Code + PKCE flow.
 *
 * Flow:
 * 1. POST /api/app/auth/login -> Redirects to IdP authorize
 * 2. Follow redirects to login form
 * 3. Parse and submit login form with credentials
 * 4. Follow redirects back to app with access_token
 *
 * @param {object} jar - k6 cookie jar to maintain session
 * @param {string} [email] - User email (defaults to VU's assigned user or TEST_EMAIL)
 * @param {string} [password] - User password (defaults to VU's assigned user or TEST_PASSWORD)
 * @returns {object} - { success: boolean, accessToken?: string, error?: string }
 */
function performOAuthLogin(jar, email = null, password = null) {
  // Get credentials - use provided values, or fall back to VU assignment
  const creds = email ? { email, password } : getCredentialsForVU();
  // Step 1: Initiate OAuth flow via BFF
  const loginRes = http.post(
    `${BASE_URL}/api/app/auth/login`,
    null,
    { redirects: 0, jar }
  );

  if (loginRes.status !== 302) {
    return { success: false, error: `Login initiation failed: ${loginRes.status}` };
  }

  // Step 2: Follow redirect to IdP authorize endpoint
  let location = loginRes.headers['Location'];
  if (!location) {
    return { success: false, error: 'No redirect location from login' };
  }

  let currentUrl = location.startsWith('http') ? location : `${BASE_URL}${location}`;
  currentUrl = rewriteUrl(currentUrl);
  let res = http.get(currentUrl, { redirects: 0, jar });

  // Manually follow redirects with URL rewriting
  while (res.status >= 300 && res.status < 400 && res.headers['Location']) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
  }

  // Step 3: Check if we're at the login form or already authenticated
  if (res.url && res.url.includes('access_token=')) {
    // Already authenticated - extract token from URL
    return extractTokenFromUrl(res.url);
  }

  // Parse the HTML to find the login form
  const doc = parseHTML(res.body);
  const form = doc.find('form');

  if (!form || form.size() === 0) {
    // Check if we ended up authenticated
    if (res.url && res.url.includes('access_token=')) {
      return extractTokenFromUrl(res.url);
    }
    return { success: false, error: `Login form not found at ${res.url}` };
  }

  // Get form action URL
  let formAction = form.attr('action') || res.url;
  formAction = resolveUrl(res.url, formAction);

  // Build form data with credentials
  const formData = {
    'Input.Email': creds.email,
    'Input.Password': creds.password,
  };

  // Add hidden fields (CSRF tokens, return URLs, etc.)
  const hiddenInputs = form.find('input[type="hidden"]');
  for (let i = 0; i < hiddenInputs.size(); i++) {
    const el = hiddenInputs.eq(i);
    const name = el.attr('name');
    const value = el.attr('value');
    if (name && value !== undefined) {
      formData[name] = value;
    }
  }

  // Step 4: Submit the login form
  formAction = rewriteUrl(formAction);
  let submitRes = http.post(formAction, formData, {
    redirects: 0,
    jar,
  });

  // Manually follow redirects with URL rewriting
  while (submitRes.status >= 300 && submitRes.status < 400 && submitRes.headers['Location']) {
    let nextUrl = resolveUrl(submitRes.url, submitRes.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    submitRes = http.get(nextUrl, { redirects: 0, jar });
  }

  // Step 5: Extract access token from final URL or response
  if (submitRes.url && submitRes.url.includes('access_token=')) {
    return extractTokenFromUrl(submitRes.url);
  }

  // Check for token in response body (some flows use fragment)
  if (submitRes.body && submitRes.body.includes('access_token')) {
    const match = submitRes.body.match(/access_token=([^&"'\s]+)/);
    if (match) {
      return { success: true, accessToken: match[1] };
    }
  }

  // Check if we're on the dashboard (token might be in localStorage, not URL)
  if (submitRes.url && submitRes.url.includes('/dashboard')) {
    // The app processed the token - we can try to get user info to validate
    return { success: true, accessToken: null, sessionBased: true };
  }

  return { success: false, error: `OAuth completed but no token found. URL: ${submitRes.url}` };
}

/**
 * Extract access_token from URL query string or fragment
 */
function extractTokenFromUrl(url) {
  try {
    // Token might be in query string or fragment
    const queryPart = url.includes('?') ? url.split('?')[1] : '';
    const fragmentPart = url.includes('#') ? url.split('#')[1] : '';
    const searchStr = queryPart || fragmentPart;

    if (searchStr) {
      const params = {};
      searchStr.split('&').forEach(pair => {
        const [key, value] = pair.split('=');
        if (key && value) {
          params[decodeURIComponent(key)] = decodeURIComponent(value);
        }
      });

      if (params.access_token) {
        return { success: true, accessToken: params.access_token };
      }
    }
  } catch (e) {
    // Fall through to error
  }
  return { success: false, error: 'Could not extract token from URL' };
}

// =============================================================================
// Registration Helper
// =============================================================================

/**
 * Performs user registration via the IdP registration form.
 *
 * Flow:
 * 1. POST /api/app/auth/login -> Redirects to IdP authorize
 * 2. Follow redirects to login form
 * 3. Click "Register" link to navigate to registration page
 * 4. Fill and submit registration form
 * 5. Follow redirects back to app with access_token
 *
 * @param {object} jar - k6 cookie jar to maintain session
 * @param {string} email - Unique email for the new user
 * @param {string} password - Password for the new user
 * @returns {object} - { success: boolean, accessToken?: string, error?: string }
 */
function performRegistration(jar, email, password) {
  // Step 1: Initiate OAuth flow via BFF (same as login)
  const loginRes = http.post(
    `${BASE_URL}/api/app/auth/login`,
    null,
    { redirects: 0, jar }
  );

  if (loginRes.status !== 302) {
    return { success: false, error: `OAuth initiation failed: ${loginRes.status}` };
  }

  // Step 2: Follow redirect to IdP authorize endpoint -> login page
  let location = loginRes.headers['Location'];
  if (!location) {
    return { success: false, error: 'No redirect location from login' };
  }

  let currentUrl = location.startsWith('http') ? location : `${BASE_URL}${location}`;
  currentUrl = rewriteUrl(currentUrl);
  let res = http.get(currentUrl, { redirects: 0, jar });

  // Manually follow redirects with URL rewriting
  while (res.status >= 300 && res.status < 400 && res.headers['Location']) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
  }

  // Step 3: Find and click the Register link
  let doc = parseHTML(res.body);

  // Try multiple selectors - the href might be case-sensitive
  let registerHref = null;
  let registerLink = doc.find('a[href*="Register"]');
  if (registerLink && registerLink.size() > 0) {
    registerHref = registerLink.attr('href');
  }

  if (!registerHref) {
    registerLink = doc.find('a[href*="register"]');
    if (registerLink && registerLink.size() > 0) {
      registerHref = registerLink.attr('href');
    }
  }

  // If still not found, search all links for one containing 'register' in href
  if (!registerHref) {
    const allLinks = doc.find('a');
    for (let i = 0; i < allLinks.size(); i++) {
      const link = allLinks.eq(i);
      const href = link.attr('href') || '';
      if (href.toLowerCase().includes('register')) {
        registerHref = href;
        break;
      }
    }
  }

  if (!registerHref) {
    return { success: false, error: `Register link not found at ${res.url}` };
  }
  registerHref = resolveUrl(res.url, registerHref);
  registerHref = rewriteUrl(registerHref);

  // Navigate to registration page
  res = http.get(registerHref, { redirects: 0, jar });

  // Follow redirects with URL rewriting
  while (res.status >= 300 && res.status < 400 && res.headers['Location']) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
  }

  // Step 4: Parse the registration form
  doc = parseHTML(res.body);
  const form = doc.find('form');

  if (!form || form.size() === 0) {
    return { success: false, error: `Registration form not found at ${res.url}` };
  }

  // Get form action URL
  let formAction = form.attr('action') || res.url;
  formAction = resolveUrl(res.url, formAction);

  // Build form data with registration details
  const formData = {
    'Input.FirstName': 'Load',
    'Input.LastName': 'Tester',
    'Input.Email': email,
    'Input.Password': password,
    'Input.ConfirmPassword': password,
  };

  // Add hidden fields (CSRF tokens, return URLs, etc.)
  const hiddenInputs = form.find('input[type="hidden"]');
  for (let i = 0; i < hiddenInputs.size(); i++) {
    const el = hiddenInputs.eq(i);
    const name = el.attr('name');
    const value = el.attr('value');
    if (name && value !== undefined) {
      formData[name] = value;
    }
  }

  // Step 5: Submit the registration form
  formAction = rewriteUrl(formAction);
  let submitRes = http.post(formAction, formData, {
    redirects: 0,
    jar,
  });

  // Manually follow redirects with URL rewriting
  while (submitRes.status >= 300 && submitRes.status < 400 && submitRes.headers['Location']) {
    let nextUrl = resolveUrl(submitRes.url, submitRes.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    submitRes = http.get(nextUrl, { redirects: 0, jar });
  }

  // Step 6: Extract access token from final URL or response
  if (submitRes.url && submitRes.url.includes('access_token=')) {
    return extractTokenFromUrl(submitRes.url);
  }

  // Check for token in response body
  if (submitRes.body && submitRes.body.includes('access_token')) {
    const match = submitRes.body.match(/access_token=([^&"'\s]+)/);
    if (match) {
      return { success: true, accessToken: match[1] };
    }
  }

  // Check if we're on the dashboard (successful registration)
  if (submitRes.url && submitRes.url.includes('/dashboard')) {
    return { success: true, accessToken: null, sessionBased: true };
  }

  // Check for validation errors in response
  if (submitRes.body && submitRes.body.includes('validation-summary-errors')) {
    return { success: false, error: 'Registration validation failed (check form data)' };
  }

  return { success: false, error: `Registration completed but no token found. URL: ${submitRes.url}` };
}

// =============================================================================
// Scenario: Public Browsing (Unauthenticated)
// =============================================================================

export function publicBrowsingFlow() {
  group('Public Browsing', () => {
    // Landing page
    const landingRes = http.get(`${BASE_URL}/`);
    check(landingRes, {
      'landing page loads': (r) => r.status === 200,
    });
    sleep(1 + Math.random() * 2);

    // Public dashboard
    const publicDashRes = http.get(`${BASE_URL}/public-dashboard`);
    check(publicDashRes, {
      'public dashboard loads': (r) => r.status === 200,
    });
    sleep(1 + Math.random() * 2);

    // Catalogue API with random page
    const page = Math.floor(Math.random() * 5);
    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=${page}&size=10`);
    check(catalogueRes, {
      'catalogue API succeeds': (r) => r.status === 200,
    });

    // Random sticker detail (if catalogue returned data)
    if (catalogueRes.status === 200) {
      try {
        const data = catalogueRes.json();
        const stickers = data.stickers || data.content || [];
        if (stickers.length > 0) {
          const sticker = stickers[Math.floor(Math.random() * stickers.length)];
          const stickerId = sticker.stickerId || sticker.id;

          const detailRes = http.get(`${BASE_URL}/api/stickers/v1/${stickerId}`);
          check(detailRes, {
            'sticker detail loads': (r) => r.status === 200,
          });

          // Sticker image (may 404 if no image uploaded)
          http.get(`${BASE_URL}/api/stickers/v1/${stickerId}/image`);
        }
      } catch (e) {
        // JSON parse error - skip sticker detail
      }
    }
    sleep(1 + Math.random() * 2);
  });
}

// =============================================================================
// Scenario: Authenticated User Flow
// =============================================================================

export function authenticatedFlow() {
  group('Authenticated Flow', () => {
    // Create a cookie jar to maintain session across requests
    const jar = http.cookieJar();

    // Perform OAuth login
    const authResult = performOAuthLogin(jar);

    const loginSuccess = check(authResult, {
      'login successful': (r) => r.success === true,
    });

    if (!loginSuccess) {
      console.warn(`Auth failed: ${authResult.error}`);
      return;
    }

    sleep(1 + Math.random() * 2);

    // Build auth headers (if we have a token)
    const authHeaders = authResult.accessToken
      ? { 'Authorization': `Bearer ${authResult.accessToken}` }
      : {};

    // Verify auth status via BFF
    const userRes = http.get(`${BASE_URL}/api/app/auth/user`, {
      headers: authHeaders,
      jar,
    });

    check(userRes, {
      'auth verification succeeds': (r) => r.status === 200,
    });

    // Try to get user ID for awards lookup
    let userId = null;
    if (userRes.status === 200) {
      try {
        const userInfo = userRes.json();
        if (userInfo.authenticated && userInfo.user) {
          userId = userInfo.user.sub || userInfo.user.id;
        }
      } catch (e) {
        // JSON parse error
      }
    }

    sleep(1 + Math.random() * 2);

    // Browse catalogue (authenticated)
    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=0&size=10`, {
      headers: authHeaders,
    });
    check(catalogueRes, {
      'authenticated catalogue loads': (r) => r.status === 200,
    });

    sleep(1 + Math.random() * 2);

    // Get user's sticker collection (if we have userId)
    if (userId) {
      const awardsRes = http.get(`${BASE_URL}/api/awards/v1/assignments/${userId}`, {
        headers: authHeaders,
      });
      check(awardsRes, {
        'user awards loads': (r) => r.status === 200 || r.status === 404,
      });
    }

    sleep(1 + Math.random() * 2);

    // Logout (50% of users)
    if (Math.random() < 0.5) {
      const logoutRes = http.post(`${BASE_URL}/api/app/auth/logout`, null, {
        jar,
        redirects: 5,
      });
      check(logoutRes, {
        'logout succeeds': (r) => r.status === 200 || r.status === 302,
      });
    }
  });
}

// =============================================================================
// Scenario: Registration Flow
// =============================================================================

export function registrationFlow() {
  group('Registration Flow', () => {
    const jar = http.cookieJar();

    // Generate unique email for this VU/iteration
    const uniqueEmail = `${REG_EMAIL_PREFIX}-${__VU}-${__ITER}-${Date.now()}@${REG_EMAIL_DOMAIN}`;
    const password = 'LoadTest2025!';

    // Perform registration
    const regResult = performRegistration(jar, uniqueEmail, password);

    const regSuccess = check(regResult, {
      'registration successful': (r) => r.success === true,
    });

    if (!regSuccess) {
      console.warn(`Registration failed: ${regResult.error}`);
      return;
    }

    sleep(1 + Math.random() * 2);

    // Build auth headers (if we have a token)
    const authHeaders = regResult.accessToken
      ? { 'Authorization': `Bearer ${regResult.accessToken}` }
      : {};

    // Verify auth status via BFF
    const userRes = http.get(`${BASE_URL}/api/app/auth/user`, {
      headers: authHeaders,
      jar,
    });

    check(userRes, {
      'new user auth verification succeeds': (r) => r.status === 200,
    });

    sleep(1 + Math.random() * 2);

    // Browse catalogue as new user
    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=0&size=10`, {
      headers: authHeaders,
    });
    check(catalogueRes, {
      'new user catalogue loads': (r) => r.status === 200,
    });

    sleep(1 + Math.random() * 2);

    // Logout
    const logoutRes = http.post(`${BASE_URL}/api/app/auth/logout`, null, {
      jar,
      redirects: 5,
    });
    check(logoutRes, {
      'new user logout succeeds': (r) => r.status === 200 || r.status === 302,
    });
  });
}

// =============================================================================
// Main Execution
// =============================================================================

export default function () {
  if (scenario === 'public') {
    publicBrowsingFlow();
  } else if (scenario === 'auth') {
    authenticatedFlow();
  } else if (scenario === 'register') {
    registrationFlow();
  } else {
    // Mixed workload: 60% public, 40% authenticated
    if (Math.random() < 0.6) {
      publicBrowsingFlow();
    } else {
      authenticatedFlow();
    }
  }
}
