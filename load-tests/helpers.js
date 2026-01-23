/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

/**
 * Shared URL helpers for k6 load tests.
 * k6 doesn't have native URL constructor, so we implement manual URL parsing.
 */

// Configuration from environment
export const BASE_URL = __ENV.TARGET_URL || 'http://traefik:80';
export const DEPLOYMENT_HOST_URL = __ENV.DEPLOYMENT_HOST_URL || 'http://localhost:8080';

/**
 * Rewrite external URLs to internal Docker network URLs.
 * OAuth redirects use the configured external hostname (e.g., localhost:8080)
 * but k6 inside Docker needs to use the internal hostname (traefik:80).
 */
export function rewriteUrl(url) {
  if (url && url.startsWith(DEPLOYMENT_HOST_URL)) {
    return url.replace(DEPLOYMENT_HOST_URL, BASE_URL);
  }
  return url;
}

/**
 * Extract origin (protocol + host) from a URL string.
 */
export function getUrlOrigin(url) {
  const match = url.match(/^(https?:\/\/[^\/]+)/);
  return match ? match[1] : '';
}

/**
 * Get the directory path from a URL (everything up to the last /).
 */
export function getUrlDirectory(url) {
  const origin = getUrlOrigin(url);
  const path = url.slice(origin.length);
  const lastSlash = path.lastIndexOf('/');
  return origin + (lastSlash >= 0 ? path.slice(0, lastSlash + 1) : '/');
}

/**
 * Resolve a relative URL against a base URL.
 */
export function resolveUrl(baseUrl, relativeUrl) {
  if (relativeUrl.startsWith('http')) {
    return relativeUrl;
  }
  if (relativeUrl.startsWith('/')) {
    return getUrlOrigin(baseUrl) + relativeUrl;
  }
  return getUrlDirectory(baseUrl) + relativeUrl;
}

/**
 * Extract access_token from URL query string or fragment.
 * Handles URLs like: http://example.com/callback?access_token=xxx
 * or: http://example.com/callback#access_token=xxx
 *
 * NOTE: k6's goja runtime lacks native URL/URLSearchParams APIs,
 * so we parse manually instead of using `new URL(url).searchParams`.
 */
export function extractTokenFromUrl(url) {
  try {
    // Split URL into base and the part after ? or #
    // Handle both query string (?access_token=) and fragment (#access_token=)
    let searchStr = '';

    const queryIndex = url.indexOf('?');
    const hashIndex = url.indexOf('#');

    if (queryIndex !== -1) {
      const endIndex = hashIndex > queryIndex ? hashIndex : url.length;
      searchStr = url.substring(queryIndex + 1, endIndex);
    } else if (hashIndex !== -1) {
      searchStr = url.substring(hashIndex + 1);
    }

    if (searchStr) {
      const params = {};
      searchStr.split('&').forEach(pair => {
        const eqIndex = pair.indexOf('=');
        if (eqIndex !== -1) {
          const key = decodeURIComponent(pair.substring(0, eqIndex));
          const value = decodeURIComponent(pair.substring(eqIndex + 1));
          params[key] = value;
        }
      });

      if (params.access_token) {
        return { success: true, accessToken: params.access_token };
      }
    }
  } catch (e) {
    console.warn('Failed to extract token from URL:', url, e);
  }
  return { success: false, error: 'Could not extract token from URL' };
}

/**
 * Maximum number of redirects to follow to prevent infinite loops.
 */
export const MAX_REDIRECTS = 10;
