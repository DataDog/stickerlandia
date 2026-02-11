/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { parseHTML } from 'k6/html';
import { SharedArray } from 'k6/data';
import {
  BASE_URL,
  DEPLOYMENT_HOST_URL,
  rewriteUrl,
  resolveUrl,
  extractTokenFromUrl,
  MAX_REDIRECTS,
} from './helpers.js';

// =============================================================================
// Configuration
// =============================================================================

const TEST_EMAIL = __ENV.TEST_EMAIL || 'user@stickerlandia.com';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || 'Stickerlandia2025!';

// Registration test users get unique emails per VU/iteration
const REG_EMAIL_PREFIX = __ENV.REG_EMAIL_PREFIX || 'loadtest';
const REG_EMAIL_DOMAIN = __ENV.REG_EMAIL_DOMAIN || 'loadtest.stickerlandia.com';

// =============================================================================
// Multi-User Pool Support
// =============================================================================

// Load user pool - SharedArray is memory-efficient across VUs
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
 * Uses user pool for sustained workloads; otherwise uses default TEST_EMAIL/TEST_PASSWORD.
 */
function getCredentialsForVU() {
  const isSustained = workload.startsWith('sustained:') || workload === 'sustained';
  if (!isSustained || users.length === 0) {
    return { email: TEST_EMAIL, password: TEST_PASSWORD };
  }
  return users[(__VU - 1) % users.length];
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
  // Sustained load profiles
  'sustained:auth': {
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
  'sustained:catalogue': {
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
  'sustained:print': {
    scenarios: {
      print_stress: {
        executor: 'constant-arrival-rate',
        rate: 5,
        timeUnit: '1s',
        duration: '5m',
        preAllocatedVUs: 10,
        maxVUs: 20,
        exec: 'printFlow',
      },
    },
  },
  sustained: {
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
        executor: 'constant-arrival-rate',
        rate: 3,
        timeUnit: '1m',
        duration: '10m',
        preAllocatedVUs: 2,
        maxVUs: 5,
        exec: 'registrationFlow',
      },
      print_flow: {
        executor: 'constant-arrival-rate',
        rate: 5,
        timeUnit: '1s',
        duration: '10m',
        preAllocatedVUs: 10,
        maxVUs: 20,
        exec: 'printFlow',
      },
    },
  },
};

const workload = __ENV.WORKLOAD || 'smoke';
const scenario = __ENV.SCENARIO || 'mixed';

if (!WORKLOADS[workload]) {
  const validWorkloads = Object.keys(WORKLOADS).join(', ');
  throw new Error(`Unknown workload "${workload}". Valid options: ${validWorkloads}`);
}

export const options = {
  ...WORKLOADS[workload],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
    checks: ['rate>0.90'],
  },
};

// =============================================================================
// OAuth Authentication Helper
// =============================================================================

/**
 * Performs the full OAuth 2.1 Authorization Code + PKCE flow.
 */
function performOAuthLogin(jar, email = null, password = null) {
  const creds = (email && password) ? { email, password } : getCredentialsForVU();

  // Step 1: Initiate OAuth flow via BFF
  const loginRes = http.post(`${BASE_URL}/api/app/auth/login`, null, { redirects: 0, jar });

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

  // Follow redirects with URL rewriting (with limit to prevent infinite loops)
  let redirectCount = 0;
  while (res.status >= 300 && res.status < 400 && res.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Step 3: Check if already authenticated
  if (res.url && res.url.includes('access_token=')) {
    return extractTokenFromUrl(res.url);
  }

  // Parse the login form
  const doc = parseHTML(res.body);
  const form = doc.find('form');

  if (!form || form.size() === 0) {
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

  // Add hidden fields
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
  let submitRes = http.post(formAction, formData, { redirects: 0, jar });

  // Follow redirects with URL rewriting
  redirectCount = 0;
  while (submitRes.status >= 300 && submitRes.status < 400 && submitRes.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(submitRes.url, submitRes.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    submitRes = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Step 5: Extract access token
  if (submitRes.url && submitRes.url.includes('access_token=')) {
    return extractTokenFromUrl(submitRes.url);
  }

  if (submitRes.body && submitRes.body.includes('access_token')) {
    const match = submitRes.body.match(/access_token=([^&"'\s]+)/);
    if (match) {
      return { success: true, accessToken: match[1] };
    }
  }

  if (submitRes.url && (submitRes.url.includes('/dashboard') || submitRes.url.includes('auth=complete'))) {
    // Exchange session for access token via BFF token endpoint
    const tokenRes = http.post(`${BASE_URL}/api/app/auth/token`, null, { jar });
    if (tokenRes.status === 200) {
      try {
        const tokenData = tokenRes.json();
        if (tokenData.access_token) {
          return { success: true, accessToken: tokenData.access_token };
        }
      } catch (e) {
        // Fall through to session-based
      }
    }
    return { success: true, accessToken: null, sessionBased: true };
  }

  return { success: false, error: `OAuth completed but no token found. URL: ${submitRes.url}` };
}

// =============================================================================
// Logout Helper
// =============================================================================

function performLogout(jar) {
  let res = http.post(`${BASE_URL}/api/app/auth/logout`, null, { jar, redirects: 0 });

  let redirectCount = 0;
  while (res.status >= 300 && res.status < 400 && res.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(res.url || `${BASE_URL}/api/app/auth/logout`, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  return res.status === 200 || res.status === 302 || (res.url && res.url.includes(BASE_URL));
}

// =============================================================================
// Registration Helper
// =============================================================================

function performRegistration(jar, email, password) {
  // Step 1: Initiate OAuth flow via BFF
  const loginRes = http.post(`${BASE_URL}/api/app/auth/login`, null, { redirects: 0, jar });

  if (loginRes.status !== 302) {
    return { success: false, error: `OAuth initiation failed: ${loginRes.status}` };
  }

  // Step 2: Follow redirect to login page
  let location = loginRes.headers['Location'];
  if (!location) {
    return { success: false, error: 'No redirect location from login' };
  }

  let currentUrl = location.startsWith('http') ? location : `${BASE_URL}${location}`;
  currentUrl = rewriteUrl(currentUrl);
  let res = http.get(currentUrl, { redirects: 0, jar });

  let redirectCount = 0;
  while (res.status >= 300 && res.status < 400 && res.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Step 3: Find Register link
  let doc = parseHTML(res.body);
  let registerHref = null;

  const allLinks = doc.find('a');
  for (let i = 0; i < allLinks.size(); i++) {
    const link = allLinks.eq(i);
    const href = link.attr('href') || '';
    if (href.toLowerCase().includes('register')) {
      registerHref = href;
      break;
    }
  }

  if (!registerHref) {
    return { success: false, error: `Register link not found at ${res.url}` };
  }

  registerHref = resolveUrl(res.url, registerHref);
  registerHref = rewriteUrl(registerHref);

  // Navigate to registration page
  res = http.get(registerHref, { redirects: 0, jar });

  redirectCount = 0;
  while (res.status >= 300 && res.status < 400 && res.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(res.url, res.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    res = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Step 4: Parse and submit registration form
  doc = parseHTML(res.body);
  const form = doc.find('form');

  if (!form || form.size() === 0) {
    return { success: false, error: `Registration form not found at ${res.url}` };
  }

  let formAction = form.attr('action') || res.url;
  formAction = resolveUrl(res.url, formAction);

  const formData = {
    'Input.FirstName': 'Load',
    'Input.LastName': 'Tester',
    'Input.Email': email,
    'Input.Password': password,
    'Input.ConfirmPassword': password,
  };

  const hiddenInputs = form.find('input[type="hidden"]');
  for (let i = 0; i < hiddenInputs.size(); i++) {
    const el = hiddenInputs.eq(i);
    const name = el.attr('name');
    const value = el.attr('value');
    if (name && value !== undefined) {
      formData[name] = value;
    }
  }

  // Step 5: Submit registration
  formAction = rewriteUrl(formAction);
  let submitRes = http.post(formAction, formData, { redirects: 0, jar });

  redirectCount = 0;
  while (submitRes.status >= 300 && submitRes.status < 400 && submitRes.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(submitRes.url, submitRes.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    submitRes = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Step 6: Extract access token
  if (submitRes.url && submitRes.url.includes('access_token=')) {
    return extractTokenFromUrl(submitRes.url);
  }

  if (submitRes.body && submitRes.body.includes('access_token')) {
    const match = submitRes.body.match(/access_token=([^&"'\s]+)/);
    if (match) {
      return { success: true, accessToken: match[1] };
    }
  }

  if (submitRes.url && submitRes.url.includes('/dashboard')) {
    return { success: true, accessToken: null, sessionBased: true };
  }

  if (submitRes.body && submitRes.body.includes('validation-summary-errors')) {
    return { success: false, error: 'Registration validation failed' };
  }

  return { success: false, error: `Registration completed but no token found. URL: ${submitRes.url}` };
}

// =============================================================================
// Scenario: Public Browsing (Unauthenticated)
// =============================================================================

export function publicBrowsingFlow() {
  group('Public Browsing', () => {
    const landingRes = http.get(`${BASE_URL}/`);
    check(landingRes, { 'landing page loads': (r) => r.status === 200 });
    sleep(1 + Math.random() * 2);

    const publicDashRes = http.get(`${BASE_URL}/public-dashboard`);
    check(publicDashRes, { 'public dashboard loads': (r) => r.status === 200 });
    sleep(1 + Math.random() * 2);

    const page = Math.floor(Math.random() * 5);
    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=${page}&size=10`);
    check(catalogueRes, { 'catalogue API succeeds': (r) => r.status === 200 });

    if (catalogueRes.status === 200) {
      try {
        const data = catalogueRes.json();
        const stickers = data.stickers || data.content || [];
        if (stickers.length > 0) {
          const sticker = stickers[Math.floor(Math.random() * stickers.length)];
          const stickerId = sticker.stickerId || sticker.id;

          const detailRes = http.get(`${BASE_URL}/api/stickers/v1/${stickerId}`);
          check(detailRes, { 'sticker detail loads': (r) => r.status === 200 });

          http.get(`${BASE_URL}/api/stickers/v1/${stickerId}/image`);
        }
      } catch (e) {
        console.warn('publicBrowsingFlow: failed to parse catalogue response', e);
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
    const jar = http.cookieJar();
    const authResult = performOAuthLogin(jar);

    const loginSuccess = check(authResult, { 'login successful': (r) => r.success === true });

    if (!loginSuccess) {
      console.warn(`Auth failed: ${authResult.error}`);
      return;
    }

    sleep(1 + Math.random() * 2);

    const authHeaders = authResult.accessToken
      ? { 'Authorization': `Bearer ${authResult.accessToken}` }
      : {};

    const userRes = http.get(`${BASE_URL}/api/app/auth/user`, { headers: authHeaders, jar });
    check(userRes, { 'auth verification succeeds': (r) => r.status === 200 });

    let userId = null;
    if (userRes.status === 200) {
      try {
        const userInfo = userRes.json();
        if (userInfo.authenticated && userInfo.user) {
          userId = userInfo.user.sub || userInfo.user.id;
        }
      } catch (e) {
        console.warn('authenticatedFlow: failed to parse user response', e);
      }
    }

    sleep(1 + Math.random() * 2);

    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=0&size=10`, { headers: authHeaders });
    check(catalogueRes, { 'authenticated catalogue loads': (r) => r.status === 200 });

    sleep(1 + Math.random() * 2);

    if (userId) {
      const awardsRes = http.get(`${BASE_URL}/api/awards/v1/assignments/${userId}`, { headers: authHeaders });
      check(awardsRes, { 'user awards loads': (r) => r.status === 200 || r.status === 404 });
    }

    sleep(1 + Math.random() * 2);

    if (Math.random() < 0.5) {
      const logoutSuccess = performLogout(jar);
      check({ success: logoutSuccess }, { 'logout succeeds': (r) => r.success === true });
    }
  });
}

// =============================================================================
// Scenario: Registration Flow
// =============================================================================

export function registrationFlow() {
  group('Registration Flow', () => {
    const jar = http.cookieJar();

    const uniqueEmail = `${REG_EMAIL_PREFIX}-${__VU}-${__ITER}-${Date.now()}@${REG_EMAIL_DOMAIN}`;
    const password = 'LoadTest2025!';

    const regResult = performRegistration(jar, uniqueEmail, password);
    const regSuccess = check(regResult, { 'registration successful': (r) => r.success === true });

    if (!regSuccess) {
      console.warn(`Registration failed: ${regResult.error}`);
      return;
    }

    sleep(1 + Math.random() * 2);

    const authHeaders = regResult.accessToken
      ? { 'Authorization': `Bearer ${regResult.accessToken}` }
      : {};

    const userRes = http.get(`${BASE_URL}/api/app/auth/user`, { headers: authHeaders, jar });
    check(userRes, { 'new user auth verification succeeds': (r) => r.status === 200 });

    sleep(1 + Math.random() * 2);

    const catalogueRes = http.get(`${BASE_URL}/api/stickers/v1?page=0&size=10`, { headers: authHeaders });
    check(catalogueRes, { 'new user catalogue loads': (r) => r.status === 200 });

    sleep(1 + Math.random() * 2);

    const logoutSuccess = performLogout(jar);
    check({ success: logoutSuccess }, { 'new user logout succeeds': (r) => r.success === true });
  });
}

// =============================================================================
// Scenario: Print Flow (Authenticated User Prints Owned Sticker)
// =============================================================================

export function printFlow() {
  group('Print Flow', () => {
    const jar = http.cookieJar();
    const authResult = performOAuthLogin(jar);

    const loginSuccess = check(authResult, { 'print: login successful': (r) => r.success === true });

    if (!loginSuccess) {
      console.warn(`Print flow auth failed: ${authResult.error}`);
      return;
    }

    sleep(1 + Math.random() * 2);

    const authHeaders = authResult.accessToken
      ? { 'Authorization': `Bearer ${authResult.accessToken}` }
      : {};

    // Step 1: Get user identity
    const userRes = http.get(`${BASE_URL}/api/app/auth/user`, { headers: authHeaders, jar });
    check(userRes, { 'print: user info loads': (r) => r.status === 200 });

    let userId = null;
    if (userRes.status === 200) {
      try {
        const userInfo = userRes.json();
        if (userInfo.authenticated && userInfo.user) {
          userId = userInfo.user.sub || userInfo.user.id;
        }
      } catch (e) {
        console.warn('printFlow: failed to parse user response', e);
      }
    }

    if (!userId) {
      console.warn('printFlow: could not determine userId, skipping');
      return;
    }

    sleep(1 + Math.random() * 2);

    // Step 2: Fetch user's owned stickers (awards)
    const awardsRes = http.get(`${BASE_URL}/api/awards/v1/assignments/${userId}`, { headers: authHeaders });
    check(awardsRes, { 'print: user awards loads': (r) => r.status === 200 || r.status === 404 });

    let ownedStickers = [];
    if (awardsRes.status === 200) {
      try {
        const awardsData = awardsRes.json();
        ownedStickers = awardsData.stickers || [];
      } catch (e) {
        console.warn('printFlow: failed to parse awards response', e);
      }
    }

    if (ownedStickers.length === 0) {
      console.warn('printFlow: user has no owned stickers, skipping print');
      return;
    }

    sleep(1 + Math.random() * 2);

    // Step 3: View a sticker from collection (simulate browsing detail page)
    const selectedSticker = ownedStickers[Math.floor(Math.random() * ownedStickers.length)];
    const stickerId = selectedSticker.stickerId || selectedSticker.id;

    const detailRes = http.get(`${BASE_URL}/api/stickers/v1/${stickerId}`, { headers: authHeaders });
    check(detailRes, { 'print: sticker detail loads': (r) => r.status === 200 });

    sleep(1 + Math.random() * 2);

    // Step 4: Discover available events and printers
    const eventsRes = http.get(`${BASE_URL}/api/print/v1/events`, { headers: authHeaders, jar });
    check(eventsRes, { 'print: events list loads': (r) => r.status === 200 });

    let events = [];
    if (eventsRes.status === 200) {
      try {
        const eventsData = eventsRes.json();
        events = eventsData.data || [];
      } catch (e) {
        console.warn('printFlow: failed to parse events response', e);
      }
    }

    if (events.length === 0) {
      console.warn('printFlow: no events available, skipping print');
      return;
    }

    const eventName = events[Math.floor(Math.random() * events.length)];

    sleep(1 + Math.random() * 2);

    // Step 5: Get printers and their status for the event
    const printersRes = http.get(`${BASE_URL}/api/print/v1/event/${encodeURIComponent(eventName)}/printers/status`, {
      headers: authHeaders,
      jar,
    });
    check(printersRes, { 'print: printer status loads': (r) => r.status === 200 });

    let printers = [];
    if (printersRes.status === 200) {
      try {
        const printersData = printersRes.json();
        const allPrinters = (printersData.data && printersData.data.printers) || [];
        // Prefer online printers, fall back to any printer
        printers = allPrinters.filter((p) => p.status === 'Online');
        if (printers.length === 0) {
          printers = allPrinters;
        }
      } catch (e) {
        console.warn('printFlow: failed to parse printers response', e);
      }
    }

    if (printers.length === 0) {
      console.warn('printFlow: no printers available for event, skipping print');
      return;
    }

    const printer = printers[Math.floor(Math.random() * printers.length)];
    const printerName = printer.printerName || printer.name;

    sleep(1 + Math.random() * 2);

    // Step 6: Submit print job for the owned sticker
    const stickerUrl = `${DEPLOYMENT_HOST_URL}/api/stickers/v1/${encodeURIComponent(stickerId)}/image`;
    const printJobPayload = JSON.stringify({
      userId: userId,
      stickerId: stickerId,
      stickerUrl: stickerUrl,
    });

    const printRes = http.post(
      `${BASE_URL}/api/print/v1/event/${encodeURIComponent(eventName)}/printer/${encodeURIComponent(printerName)}/jobs`,
      printJobPayload,
      {
        headers: { ...authHeaders, 'Content-Type': 'application/json' },
        jar,
      },
    );
    check(printRes, {
      'print: job submitted': (r) => r.status === 201,
    });

    if (printRes.status === 201) {
      try {
        const printData = printRes.json();
        const jobId = printData.data && printData.data.printJobId;
        if (jobId) {
          console.log(`printFlow: submitted job ${jobId} for sticker ${stickerId}`);
        }
      } catch (e) {
        // Response parsed fine, just logging
      }
    } else {
      console.warn(`printFlow: print job submission failed with status ${printRes.status}`);
    }

    sleep(1 + Math.random() * 2);

    // Step 7: Logout
    if (Math.random() < 0.5) {
      const logoutSuccess = performLogout(jar);
      check({ success: logoutSuccess }, { 'print: logout succeeds': (r) => r.success === true });
    }
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
  } else if (scenario === 'print') {
    printFlow();
  } else {
    // Mixed workload: 50% public, 30% authenticated, 20% print
    const roll = Math.random();
    if (roll < 0.5) {
      publicBrowsingFlow();
    } else if (roll < 0.8) {
      authenticatedFlow();
    } else {
      printFlow();
    }
  }
}
