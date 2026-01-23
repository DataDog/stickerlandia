# Load Testing

k6-based load simulator for Stickerlandia.

## Quick Start

```bash
# Smoke test (2 users, 30 seconds)
mise run load:smoke

# Load test (ramp to 30 users over 10 minutes)
mise run load:test

# Sustained load with multi-user pool
mise run load:sustained
```

## Available Tasks

| Task | Description |
|------|-------------|
| `load:smoke` | Quick validation (2 VUs, 30s) |
| `load:smoke:public` | Public endpoints only |
| `load:smoke:auth` | Authenticated flows only |
| `load:test` | Ramp to 30 users over 10 minutes |
| `load:sustained` | Full cycle: provision users, run sustained load, cleanup |
| `load:sustained:run` | Run sustained test (assumes services running) |
| `load:start` | Start services only |
| `load:stop` | Stop services and cleanup |
| `load:provision-users` | Register users from pool |

## Multi-User Testing

For sustained load tests with multiple concurrent users, create `load-tests/data/users.json`:

```json
{
  "users": [
    {"email": "loadtest-001@loadtest.stickerlandia.com", "password": "LoadTest2026!"},
    {"email": "loadtest-002@loadtest.stickerlandia.com", "password": "LoadTest2026!"}
  ]
}
```

This file is gitignored. Run `mise run load:provision-users` once to register users before running auth tests.

## Configuration

| Variable | Default |
|----------|---------|
| `WORKLOAD` | `smoke` |
| `SCENARIO` | `mixed` |
| `TEST_EMAIL` | `user@stickerlandia.com` |
| `TEST_PASSWORD` | `Stickerlandia2025!` |

## Troubleshooting

- **OAuth fails**: Verify test user exists and services are healthy (`docker compose ps`)
- **High latency**: Auth flows include multiple redirects; 1s p95 is expected
