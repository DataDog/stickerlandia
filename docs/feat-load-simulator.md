# feat: Load Simulator for Core User Flows

**Date:** 2026-01-22
**Type:** Enhancement
**Status:** Implemented

---

## Overview

A k6-based load simulator that tests public endpoints, authenticated API flows (OAuth 2.1 PKCE), and user registration under concurrent load. Runs via Docker Compose with full service orchestration and Datadog APM integration.

## Problem Statement

No visibility into system behavior under load. Need validation that the API can handle concurrent requests - including authenticated flows - without falling over.

## Solution Summary

A **k6-based load simulator** that:
- Tests public endpoints, authenticated endpoints, and registration flows
- Implements full OAuth 2.1 authorization code flow with PKCE
- Handles Docker networking (URL rewriting for internal/external hostnames)
- Runs via Docker Compose with automatic service startup and teardown
- Disables rate limiting during load tests
- Includes Datadog agent for APM visibility
- Reports pass/fail based on response time and error rate thresholds

---

## Implementation Details

### Files Created

```
load-tests/
├── load-test.js              # Main test (~450 lines)
└── README.md                 # Usage documentation

docker-compose.load-test.yml  # Load test service + web-backend override
```

### Key Technical Challenges Solved

#### 1. Docker Network URL Rewriting

OAuth redirects use the external hostname (`localhost:8080`) which isn't reachable from inside Docker. The load simulator rewrites URLs to use the internal hostname (`traefik:80`):

```javascript
const BASE_URL = __ENV.TARGET_URL || 'http://traefik:80';
const EXTERNAL_URL = __ENV.EXTERNAL_URL || 'http://localhost:8080';

function rewriteUrl(url) {
  if (url && url.startsWith(EXTERNAL_URL)) {
    return url.replace(EXTERNAL_URL, BASE_URL);
  }
  return url;
}
```

#### 2. k6-Compatible URL Parsing

k6 doesn't have the native `URL` constructor. Custom helpers handle URL resolution:

```javascript
function getUrlOrigin(url) {
  const match = url.match(/^(https?:\/\/[^\/]+)/);
  return match ? match[1] : '';
}

function resolveUrl(baseUrl, relativeUrl) {
  if (relativeUrl.startsWith('http')) return relativeUrl;
  if (relativeUrl.startsWith('/')) return getUrlOrigin(baseUrl) + relativeUrl;
  return getUrlDirectory(baseUrl) + relativeUrl;
}
```

#### 3. k6-Compatible Form Iteration

k6's `each()` callback works differently than jQuery. Use `size()` and `eq()` instead:

```javascript
const hiddenInputs = form.find('input[type="hidden"]');
for (let i = 0; i < hiddenInputs.size(); i++) {
  const el = hiddenInputs.eq(i);
  const name = el.attr('name');
  const value = el.attr('value');
  if (name && value !== undefined) {
    formData[name] = value;
  }
}
```

#### 4. Manual Redirect Following

To apply URL rewriting during OAuth redirect chains, redirects are followed manually:

```javascript
let res = http.get(currentUrl, { redirects: 0, jar });

while (res.status >= 300 && res.status < 400 && res.headers['Location']) {
  let nextUrl = resolveUrl(res.url, res.headers['Location']);
  nextUrl = rewriteUrl(nextUrl);
  res = http.get(nextUrl, { redirects: 0, jar });
}
```

---

## Scenarios

### Public Browsing Flow
- `GET /` - Landing page
- `GET /public-dashboard` - Public statistics
- `GET /api/stickers/v1?page={n}&size=10` - Catalogue API
- `GET /api/stickers/v1/{id}` - Sticker detail
- `GET /api/stickers/v1/{id}/image` - Sticker image

### Authenticated Flow
- `POST /api/app/auth/login` - Initiate OAuth
- OAuth redirect dance (authorize -> login form -> callback)
- `GET /api/app/auth/user` - Verify authentication
- `GET /api/stickers/v1` - Catalogue (authenticated)
- `GET /api/awards/v1/assignments/{userId}` - User's stickers
- `POST /api/app/auth/logout` - Logout (50% of users)

### Registration Flow
- `POST /api/app/auth/login` - Initiate OAuth (redirects to IdP)
- `GET /Account/Login` - Login page with Register link
- `GET /Account/Register` - Registration form
- `POST /Account/Register` - Submit registration (FirstName, LastName, Email, Password)
- OAuth callback with access_token
- `GET /api/app/auth/user` - Verify new user authenticated
- `GET /api/stickers/v1` - Browse catalogue as new user
- `POST /api/app/auth/logout` - Logout

---

## Docker Compose Configuration

```yaml
# docker-compose.load-test.yml
services:
  # Disable rate limiting for load testing
  web-backend:
    environment:
      - SKIP_RATE_LIMIT=true

  load-simulator:
    image: grafana/k6:latest
    volumes:
      - ./load-tests:/scripts
    environment:
      - TARGET_URL=http://traefik:80
      - EXTERNAL_URL=http://localhost:8080
      - WORKLOAD=${WORKLOAD:-smoke}
      - SCENARIO=${SCENARIO:-mixed}
      - TEST_EMAIL=${TEST_EMAIL:-user@stickerlandia.com}
      - TEST_PASSWORD=${TEST_PASSWORD:-Stickerlandia2025!}
    command: run /scripts/load-test.js
    depends_on:
      traefik:
        condition: service_healthy
      user-management:
        condition: service_healthy
      user-management-worker:
        condition: service_healthy
      sticker-catalogue:
        condition: service_healthy
      sticker-award:
        condition: service_healthy
      web-backend:
        condition: service_started
      datadog-agent:
        condition: service_started
    profiles:
      - load-test
```

---

## Mise Tasks

All tasks start all services, run the load test, then tear down:

```toml
[tasks."load:smoke"]
description = "Run smoke load test (2 users, 30 seconds, mixed flows)"
run = """
docker compose -f docker-compose.yml -f docker-compose.load-test.yml --profile load-test up -d
docker compose -f docker-compose.yml -f docker-compose.load-test.yml --profile load-test run --rm load-simulator
docker compose -f docker-compose.yml -f docker-compose.load-test.yml --profile load-test down -v
"""

[tasks."load:smoke:public"]
description = "Run smoke test on public endpoints only"

[tasks."load:smoke:auth"]
description = "Run smoke test on authenticated flows only"

[tasks."load:smoke:register"]
description = "Run smoke test on registration flow"

[tasks."load:test"]
description = "Run load test (ramp to 30 users over 10 minutes)"

[tasks."load:test:auth"]
description = "Run load test on authenticated flows only"
```

---

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `TARGET_URL` | Internal Docker network URL | `http://traefik:80` |
| `EXTERNAL_URL` | External URL (for OAuth redirect rewriting) | `http://localhost:8080` |
| `WORKLOAD` | `smoke` or `load` | `smoke` |
| `SCENARIO` | `public`, `auth`, `register`, or `mixed` | `mixed` |
| `TEST_EMAIL` | Test user email (for auth scenario) | `user@stickerlandia.com` |
| `TEST_PASSWORD` | Test user password (for auth scenario) | `Stickerlandia2025!` |
| `REG_EMAIL_PREFIX` | Registration email prefix | `loadtest` |
| `REG_EMAIL_DOMAIN` | Registration email domain | `loadtest.stickerlandia.com` |

---

## Workloads

| Workload | VUs | Duration | Use Case |
|----------|-----|----------|----------|
| `smoke` | 2 | 30s | Quick validation |
| `load` | 10->30 | 10m | Sustained load testing |

---

## Thresholds

| Metric | Threshold | Description |
|--------|-----------|-------------|
| `http_req_failed` | < 1% | Error rate |
| `http_req_duration` | p95 < 1000ms | Response time (auth flows are slower) |
| `checks` | > 90% | Check pass rate |

---

## Usage

```bash
# Quick smoke test (mixed public + auth flows)
mise run load:smoke

# Public endpoints only
mise run load:smoke:public

# Authenticated flows only
mise run load:smoke:auth

# Registration flow
mise run load:smoke:register

# Full load test (10 minutes, 30 VUs)
mise run load:test

# Load test on auth flows only
mise run load:test:auth
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Docker Network                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────┐     ┌─────────────┐     ┌──────────────────┐          │
│  │   k6    │────▶│ web-backend │────▶│ user-management  │          │
│  │         │     │   (BFF)     │     │    (OpenIddict)  │          │
│  └─────────┘     └─────────────┘     └──────────────────┘          │
│       │                                      │                       │
│       │ Uses traefik:80 internally           │                       │
│       │ Rewrites localhost:8080 redirects    │                       │
│       │                                      │                       │
│       │ 1. POST /api/app/auth/login          │                       │
│       │──────────────────────────────────────▶                       │
│       │ 2. Redirect to authorize             │                       │
│       │◀─────────────────────────────────────│                       │
│       │ 3. Redirect to login form            │                       │
│       │◀─────────────────────────────────────│                       │
│       │ 4. POST credentials                  │                       │
│       │──────────────────────────────────────▶                       │
│       │ 5. Redirect with auth code           │                       │
│       │◀─────────────────────────────────────│                       │
│       │ 6. GET callback with code            │                       │
│       │──────────────────────────────────────▶                       │
│       │ 7. Token exchange (BFF <-> IdP)      │                       │
│       │ 8. Redirect with access_token        │                       │
│       │◀─────────────────────────────────────│                       │
│                                                                      │
│  ┌─────────────────┐                                                │
│  │ datadog-agent   │◀── APM traces from all services                │
│  └─────────────────┘                                                │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Troubleshooting

### OAuth login fails
1. Verify the test user exists: `user@stickerlandia.com` is seeded by migrations
2. Check that all services are healthy: `docker compose ps`
3. Try logging in manually via the web UI

### Rate limiting (429 errors)
The load-test compose file sets `SKIP_RATE_LIMIT=true` for web-backend. If you still see 429s:
- Ensure you're using the load-test compose overlay
- Check that web-backend restarted with the new env var

### High response times
- Auth flows include multiple redirects - 1s p95 is expected
- Public endpoints should be < 500ms
- Check service health and database performance

### Register link not found
The registration flow searches for links containing "register" (case-insensitive) in the href. If the login page structure changes, update the selector logic in `performRegistration()`.

---

## Known Limitations

1. **Single test user for auth** - All VUs share the same credentials for the auth scenario. Registration creates unique users per VU/iteration.

2. **No token caching** - Each VU performs a fresh login. For more realistic tests, consider caching tokens at the VU level across iterations.

3. **Registration creates real users** - The registration flow creates actual users in the database with emails like `loadtest-{VU}-{ITER}-{timestamp}@loadtest.stickerlandia.com`.

---

## References

- E2E test user: `user@stickerlandia.com` / `Stickerlandia2025!`
- OAuth flow: `web-backend/server.js`
- Login form: `user-management/.../Areas/Auth/Pages/Login.cshtml`
- Registration form: `user-management/.../Areas/Auth/Pages/Register.cshtml`
- k6 HTML parsing: https://k6.io/docs/javascript-api/k6-html/
- k6 cookie jar: https://k6.io/docs/javascript-api/k6-http/cookiejar/
