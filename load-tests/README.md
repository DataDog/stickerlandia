# Load Testing

k6-based load simulator for Stickerlandia that tests public endpoints, authenticated user flows (OAuth 2.1 PKCE), and user registration.

## Quick Start

```bash
# Run smoke test (2 users, 30 seconds, mixed flows)
mise run load:smoke

# Run load test (ramp to 30 users over 10 minutes)
mise run load:test
```

All mise tasks automatically:
1. Start all services with `docker compose up -d`
2. Run the load simulator
3. Tear down with `docker compose down -v`

## Scenarios

| Scenario | Command | Description |
|----------|---------|-------------|
| Mixed | `mise run load:smoke` | 60% public browsing, 40% authenticated flows |
| Public Only | `mise run load:smoke:public` | Unauthenticated endpoints only |
| Auth Only | `mise run load:smoke:auth` | OAuth login + authenticated APIs |
| Register | `mise run load:smoke:register` | New user registration flow |

## Workloads

| Workload | VUs | Duration | Use Case |
|----------|-----|----------|----------|
| `smoke` | 2 | 30s | Quick validation |
| `load` | 10→30 | 10m | Sustained load testing |
| `gameday:auth` | 50-100 | 5m | Heavy auth load (50 RPS) |
| `gameday:catalogue` | 100-150 | 5m | Heavy catalogue browsing (100 RPS) |
| `gameday:sustained` | 80-130 | 10m | Sustained load across all services |

## GameDay Load Testing

For GameDay demos, use multi-user load testing with predefined profiles:

```bash
# Sustained load across all services (default)
mise run load:gameday

# Heavy auth load (50 RPS on login/logout)
mise run load:gameday WORKLOAD=gameday:auth

# Heavy catalogue browsing (100 RPS on sticker API)
mise run load:gameday WORKLOAD=gameday:catalogue
```

### Multi-User Pool Setup

GameDay profiles use a pool of 50 test users to simulate realistic concurrent load. Create the user pool file:

```bash
# The file should be at load-tests/data/users.json
# See the template in plans/feat-gameday-load-testing-enhancements.md
```

**Important:** For auth-based GameDay profiles (`gameday:auth`, `gameday:sustained`), users must be **registered first** before they can log in. You can:
1. Run the registration flow first: `mise run load:smoke:register` (creates unique users per iteration)
2. Manually register users via the web UI
3. Use `gameday:catalogue` which doesn't require authentication

**Note:** Load test traffic appears in Datadog APM as normal requests. This is intentional - it demonstrates the system under realistic load conditions.

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

## User Flows Tested

### Public Browsing
- `GET /` - Landing page
- `GET /public-dashboard` - Public statistics
- `GET /api/stickers/v1?page={n}&size=10` - Catalogue API
- `GET /api/stickers/v1/{id}` - Sticker detail
- `GET /api/stickers/v1/{id}/image` - Sticker image

### Authenticated Flow
- `POST /api/app/auth/login` - Initiate OAuth
- OAuth redirect dance (authorize → login form → callback)
- `GET /api/app/auth/user` - Verify authentication
- `GET /api/stickers/v1` - Catalogue (authenticated)
- `GET /api/awards/v1/assignments/{userId}` - User's stickers
- `POST /api/app/auth/logout` - Logout (50% of users)

### Registration Flow
- `POST /api/app/auth/login` - Initiate OAuth (redirects to IdP)
- Navigate to registration page via "Register" link
- `POST /Account/Register` - Submit registration form
- OAuth callback with access_token
- `GET /api/app/auth/user` - Verify new user authenticated
- `GET /api/stickers/v1` - Browse catalogue as new user
- `POST /api/app/auth/logout` - Logout

## Thresholds

| Metric | Threshold | Description |
|--------|-----------|-------------|
| `http_req_failed` | < 1% | Error rate |
| `http_req_duration` | p95 < 1000ms | Response time |
| `checks` | > 90% | Check pass rate |

## Running Manually

```bash
# Start all services first
docker compose -f docker-compose.yml -f docker-compose.load-test.yml --profile load-test up -d

# Run load test
docker compose -f docker-compose.yml -f docker-compose.load-test.yml \
  --profile load-test run --rm load-simulator

# With custom settings
docker compose -f docker-compose.yml -f docker-compose.load-test.yml \
  --profile load-test run --rm \
  -e WORKLOAD=load \
  -e SCENARIO=auth \
  load-simulator

# Tear down
docker compose -f docker-compose.yml -f docker-compose.load-test.yml --profile load-test down -v
```

## Troubleshooting

### OAuth login fails
1. Verify the test user exists: `user@stickerlandia.com` is seeded by migrations
2. Check that all services are healthy: `docker compose ps`
3. Try logging in manually via the web UI

### Rate limiting (429 errors)
The `docker-compose.load-test.yml` automatically sets `SKIP_RATE_LIMIT=true` for web-backend, which disables rate limiting during load tests. If you still see 429s:
- Ensure you're using the load-test compose overlay
- Check that web-backend has restarted with the new environment variable

### High response times
- Auth flows include multiple redirects - 1s p95 is expected
- Public endpoints should be < 500ms
- Check service health and database performance

### Register link not found
The registration flow searches for links containing "register" (case-insensitive) in the href. If the login page structure changes, update the selector logic in `performRegistration()`.

## Implementation Notes

### Docker Network URL Rewriting
OAuth redirects use the external hostname (`localhost:8080`) which isn't reachable from inside Docker. The load simulator rewrites URLs to use the internal hostname (`traefik:80`).

### k6 Limitations
- No native `URL` constructor - custom URL parsing helpers are used
- `each()` callback works differently than jQuery - use `size()` and `eq()` for iteration
- Redirects are followed manually to apply URL rewriting

### Datadog Integration
The load-test profile includes the Datadog agent, so all services send APM traces during load tests for observability.

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
│       │ 7. Token exchange (BFF ↔ IdP)        │                       │
│       │ 8. Redirect with access_token        │                       │
│       │◀─────────────────────────────────────│                       │
│                                                                      │
│  ┌─────────────────┐                                                │
│  │ datadog-agent   │◀── APM traces from all services                │
│  └─────────────────┘                                                │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```
