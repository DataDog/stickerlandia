# Load Testing

k6-based load tests for Stickerlandia.

## Running Tests

```bash
mise run load:smoke          # Quick test: 2 users, 30 seconds
mise run load:test           # Full test: ramp to 30 users over 10 minutes
mise run load:sustained      # Multi-user: provision users, run 10 min sustained load
```

Each command starts services, runs the test, then cleans up.

### Test Scenarios

```bash
mise run load:smoke:public   # Public endpoints only (no auth)
mise run load:smoke:auth     # Authenticated flows only
mise run load:smoke:register # Registration flow
```

### Keep Services Running

For iterative testing:

```bash
mise run load:start              # Start services
mise run load:sustained:run      # Run test (repeat as needed)
mise run load:stop               # Stop and cleanup
```

## Multi-User Pool

For sustained tests with multiple concurrent users:

1. Create `load-tests/data/users.json`:

```json
{
  "users": [
    {"email": "loadtest-001@example.com", "password": "LoadTest2026!"},
    {"email": "loadtest-002@example.com", "password": "LoadTest2026!"}
  ]
}
```

2. Provision users once: `mise run load:provision-users`

3. Run tests: `mise run load:sustained`

## Configuration

Override defaults with environment variables:

```bash
WORKLOAD=load SCENARIO=auth mise run load:smoke
```

| Variable | Default | Options |
|----------|---------|---------|
| `WORKLOAD` | `smoke` | `smoke`, `load`, `sustained`, `sustained:auth`, `sustained:catalogue` |
| `SCENARIO` | `mixed` | `mixed`, `public`, `auth`, `register` |
