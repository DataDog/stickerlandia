/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

/**
 * Provision test users by registering them via the IdP.
 * Run this once before running multi-user auth-based load tests.
 *
 * Usage: mise run load:provision-users
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { parseHTML } from 'k6/html';
import { SharedArray } from 'k6/data';
import {
  BASE_URL,
  rewriteUrl,
  resolveUrl,
  MAX_REDIRECTS,
} from './helpers.js';

// =============================================================================
// Configuration
// =============================================================================

// Load user pool
const users = new SharedArray('users', function () {
  return JSON.parse(open('./data/users.json')).users;
});

// Use few VUs to avoid overwhelming the IdP
export const options = {
  scenarios: {
    register_users: {
      executor: 'per-vu-iterations',
      vus: 5,
      iterations: Math.ceil(users.length / 5),
      maxDuration: '10m',
    },
  },
  thresholds: {
    checks: ['rate>0.80'], // Allow some failures (user might already exist)
  },
};

// =============================================================================
// Registration Flow
// =============================================================================

function registerUser(email, password) {
  const jar = http.cookieJar();

  // Step 1: Initiate OAuth flow
  const loginRes = http.post(`${BASE_URL}/api/app/auth/login`, null, { redirects: 0, jar });

  if (loginRes.status !== 302) {
    return { success: false, error: `OAuth initiation failed: ${loginRes.status}` };
  }

  // Step 2: Follow redirects to login page
  let location = loginRes.headers['Location'];
  if (!location) {
    return { success: false, error: 'No redirect location' };
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
    return { success: false, error: 'Register link not found' };
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
    return { success: false, error: 'Registration form not found' };
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

  // Submit registration
  formAction = rewriteUrl(formAction);
  let submitRes = http.post(formAction, formData, { redirects: 0, jar });

  redirectCount = 0;
  while (submitRes.status >= 300 && submitRes.status < 400 && submitRes.headers['Location'] && redirectCount < MAX_REDIRECTS) {
    let nextUrl = resolveUrl(submitRes.url, submitRes.headers['Location']);
    nextUrl = rewriteUrl(nextUrl);
    submitRes = http.get(nextUrl, { redirects: 0, jar });
    redirectCount++;
  }

  // Check for success
  if (submitRes.url && (submitRes.url.includes('access_token=') || submitRes.url.includes('/dashboard'))) {
    return { success: true };
  }

  // Check for "already exists" error (which is acceptable)
  if (submitRes.body && submitRes.body.includes('already')) {
    return { success: true, alreadyExists: true };
  }

  return { success: false, error: `Registration failed. URL: ${submitRes.url}` };
}

// =============================================================================
// Main Execution
// =============================================================================

export default function () {
  // Calculate which user this VU/iteration should register
  const numVUs = 5;
  const userIndex = (__VU - 1) + (__ITER * numVUs);

  if (userIndex >= users.length) {
    console.log(`VU ${__VU} iter ${__ITER}: No more users to register`);
    return;
  }

  const user = users[userIndex];
  console.log(`VU ${__VU}: Registering ${user.email}...`);

  const result = registerUser(user.email, user.password);

  const success = check(result, {
    'registration successful': (r) => r.success === true,
  });

  if (success) {
    if (result.alreadyExists) {
      console.log(`VU ${__VU}: ${user.email} already exists (OK)`);
    } else {
      console.log(`VU ${__VU}: ${user.email} registered successfully`);
    }
  } else {
    console.warn(`VU ${__VU}: Failed to register ${user.email}: ${result.error}`);
  }

  // Delay between registrations to avoid overwhelming the IdP
  sleep(2);
}
