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
| `gameday:auth` | 20-40 | 5m | Auth load (10 RPS) |
| `gameday:catalogue` | 100-150 | 5m | Heavy catalogue browsing (100 RPS) |
| `gameday:sustained` | 50-90 | 10m | Auth (10 RPS) + browsing (50 RPS) + registration (3/min) |

## Datadog Product Features

When running the load test, there are several Datadog products that will be populated with data providing the environment is correctly configured:

- [APM](https://app.datadoghq.com/apm/traces?query=env%3Adevelopment)
- [Logs](https://app.datadoghq.com/logs?query=env%3Adevelopment)
- [RUM](https://app.datadoghq.com/rum)
- [Data Streams Monitoring](https://app.datadoghq.com/data-streams)
- [Database Monitoring](https://app.datadoghq.com/databases)
- [Profiling](https://app.datadoghq.com/profiling/explorer?query=env%3Adevelopment)

## GameDay Load Testing

For GameDay demos, use multi-user load testing with predefined profiles that simulate realistic concurrent load across multiple users.

### Quick Start (Full Cycle)

```bash
# Run sustained load test (provisions users, runs test, cleans up)
mise run load:gameday

# Run with a specific profile
WORKLOAD=gameday:auth mise run load:gameday
WORKLOAD=gameday:catalogue mise run load:gameday
```

### GameDay Profiles

| Profile | Auth | Browse | Registration | Duration |
|---------|------|--------|--------------|----------|
| `gameday:auth` | 10 RPS | - | - | 5m |
| `gameday:catalogue` | - | 100 RPS | - | 5m |
| `gameday:sustained` | 10 RPS | 50 RPS | 3/min | 10m |

### Multi-User Pool Setup

GameDay profiles use a pool of test users for realistic concurrent auth testing. Create `load-tests/data/users.json`:

```json
{
  "users": [
    {"email": "loadtest-001@loadtest.stickerlandia.com", "password": "LoadTest2026!"},
    {"email": "loadtest-002@loadtest.stickerlandia.com", "password": "LoadTest2026!"},
    {"email": "loadtest-003@loadtest.stickerlandia.com", "password": "LoadTest2026!"}
  ]
}
```

This file is gitignored. VUs are assigned users round-robin from the pool.

### Advanced Usage (Keep Services Running)

For iterative testing or debugging, keep services running between test runs:

```bash
# Start services and wait for healthy
mise run load:start

# Provision users (run once per fresh database)
mise run load:provision-users

# Run GameDay test (can repeat without reprovisioning)
mise run load:gameday:run
WORKLOAD=gameday:auth mise run load:gameday:run

# When done, clean up
mise run load:stop
```

### Mise Tasks Reference

| Task | Description |
|------|-------------|
| `load:gameday` | Full cycle: start → provision → test → stop |
| `load:gameday:run` | Run test only (assumes services running) |
| `load:start` | Start services and wait for healthy |
| `load:stop` | Stop services and remove volumes |
| `load:provision-users` | Register pool users via IdP |

### User Provisioning

The `load:provision-users` task registers users from `users.json` via the IdP registration flow:

- Runs 5 parallel registrations with 2s delay between batches
- Idempotent: already-registered users are detected and skipped
- Run once per fresh database before auth-based GameDay tests

```bash
# Provision users (services must be running)
mise run load:start
mise run load:provision-users
```

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


## Troubleshooting

### OAuth login fails
1. Verify the test user exists: `user@stickerlandia.com` is seeded by migrations
2. Check that all services are healthy: `docker compose ps`
3. Try logging in manually via the web UI

### Rate limiting (429 errors)
The `docker-compose.load-test.yml` automatically sets `SKIP_RATE_LIMIT=true` for web-backend and `RateLimiting__Enabled=false` for user-management, which disables rate limiting during load tests. If you still see 429s:
- Ensure you're using the load-test compose overlay
- Check that both the web-backend and user-management services restarted with the correct environment variables

### High response times
- Auth flows include multiple redirects - 1s p95 is expected
- Public endpoints should be < 500ms
- Check service health and database performance

### Register link not found
The registration flow searches for links containing "register" (case-insensitive) in the href. If the login page structure changes, update the selector logic in `performRegistration()`.
