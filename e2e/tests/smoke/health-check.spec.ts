import { test, expect } from '@playwright/test';

test.describe('Smoke Tests', () => {
  test('frontend is accessible', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.status()).toBeLessThan(400);
  });

  test('frontend renders without errors', async ({ page }) => {
    // Listen for console errors
    const errors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Check that the main content loaded
    await expect(page.locator('h1')).toBeVisible();

    // No critical console errors (filter out expected ones)
    const criticalErrors = errors.filter(
      (e) => !e.includes('favicon') && !e.includes('404')
    );
    expect(criticalErrors).toHaveLength(0);
  });

  test('API health endpoint responds', async ({ request }) => {
    // Test the web-backend health endpoint
    const response = await request.get('/api/health');
    expect(response.ok()).toBeTruthy();
  });

  test('static assets load correctly', async ({ page }) => {
    const failedRequests: string[] = [];

    page.on('requestfailed', (request) => {
      failedRequests.push(request.url());
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Filter out expected failures (like optional analytics)
    const criticalFailures = failedRequests.filter(
      (url) => !url.includes('analytics') && !url.includes('tracking')
    );
    expect(criticalFailures).toHaveLength(0);
  });
});
