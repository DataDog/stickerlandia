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

# Against AWS (uses DEPLOYMENT_HOST_URL from .env)
mise run aws:ui-test

# Interactive mode
mise run compose:ui-test:ui

# Against custom URL
BASE_URL=https://example.com npx playwright test
```

## Configuration

Copy `.env.example` to `.env`:

```bash
BASE_URL=http://localhost:8080
TEST_USER_EMAIL=user@stickerlandia.com
TEST_USER_PASSWORD=Stickerlandia2025!
```

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
