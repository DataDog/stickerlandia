/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import AuthService from '../services/AuthService';

/**
 * Wrapper around fetch that automatically adds the Authorization header
 * with the JWT token if the user is authenticated.
 *
 * @param {string} url - The URL to fetch
 * @param {RequestInit} options - Fetch options
 * @returns {Promise<Response>} - The fetch response
 */
export async function authFetch(url, options = {}) {
  const tokenData = AuthService.getStoredToken();
  const headers = new Headers(options.headers || {});

  if (tokenData?.access_token && AuthService.isTokenValid()) {
    headers.set('Authorization', `Bearer ${tokenData.access_token}`);
  }

  return fetch(url, { ...options, headers });
}
