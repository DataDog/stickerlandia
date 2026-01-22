# E2E Tests

Playwright-based end-to-end tests for Stickerlandia. Can be pointed at any deployment.

## Test Suites

| Suite | Description |
|-------|-------------|
| `public` | Landing page, public dashboard |
| `auth` | Login and registration flows |
| `authenticated` | Protected routes (dashboard, collection, sticker detail) |
| `api` | Health checks, sticker catalogue endpoints |

## Running Tests

```bash
# Against local Docker Compose
mise run compose:ui-test

# Against AWS (fetches URL from CDK stack output)
mise run aws:ui-test

# Interactive mode
mise run compose:ui-test:ui

# Against custom URL
BASE_URL=https://example.com npx playwright test
```

## Configuration

Test credentials can be overridden via `TEST_USER_EMAIL` and `TEST_USER_PASSWORD` env vars.

## Structure

```
e2e/
├── tests/
│   ├── auth/           # Login, registration, logout
│   ├── authenticated/  # Protected route tests
│   ├── public/         # Public page tests
│   └── api/            # API endpoint tests
├── fixtures/           # Shared test helpers
├── page-objects/       # Page object models
└── playwright.config.ts
```
