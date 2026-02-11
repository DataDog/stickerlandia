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
mise run load:smoke:print    # Print flow (authenticated user prints owned sticker)
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

## Validating Telemetry in Datadog

After running load tests, verify data appears in Datadog:

| Product | Link | What to Check |
|---------|------|---------------|
| APM | [Traces](https://app.datadoghq.com/apm/traces?query=env%3Adevelopment) | Service traces for all requests |
| Logs | [Logs](https://app.datadoghq.com/logs?query=env%3Adevelopment) | Application logs from all services |
| RUM | [Sessions](https://app.datadoghq.com/rum/sessions) | Browser sessions (requires frontend interaction) |
| DSM | [Data Streams](https://app.datadoghq.com/data-streams) | Kafka message flow between services |
| DBM | [Databases](https://app.datadoghq.com/databases) | Query performance metrics |
| Profiling | [Profiles](https://app.datadoghq.com/profiling/explorer?query=env%3Adevelopment) | CPU/memory profiles |

Filter by `env:development` to see load test data.

## Configuration

Override defaults with environment variables:

```bash
WORKLOAD=load SCENARIO=auth mise run load:smoke
```

| Variable | Default | Options |
|----------|---------|---------|
| `WORKLOAD` | `smoke` | `smoke`, `load`, `sustained`, `sustained:auth`, `sustained:catalogue`, `sustained:print` |
| `SCENARIO` | `mixed` | `mixed`, `public`, `auth`, `register`, `print` |
