import { test, expect } from '@playwright/test';
import { LandingPage } from '../../page-objects/landing.page';
import { performLogin } from '../../fixtures/auth.fixture';
import { validateLoggedInState } from '../../fixtures/post-login-validation';
import { LOGIN_SELECTORS } from '../../fixtures/selectors';

test.describe('Login Flow', () => {
  test('clicking Start Collecting initiates OAuth flow', async ({ page }) => {
    const baseUrl = process.env.BASE_URL || 'http://localhost:8080';
    const landingPage = new LandingPage(page);
    await landingPage.goto();

    // Click the login button
    await landingPage.clickStartCollecting();

    // Wait for navigation away from home page
    await page.waitForURL((url) => url.href !== baseUrl && url.href !== `${baseUrl}/`, {
      timeout: 30000,
    });

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Should see login form or be redirected to auth
    const emailInput = page.locator(LOGIN_SELECTORS.emailInput).first();

    // Either we see the login form, or we got redirected (if already authenticated)
    try {
      await expect(emailInput).toBeVisible({ timeout: 20000 });
    } catch {
      // If no email input, check we're on an auth-related URL or got redirected back
      const url = page.url().toLowerCase();
      expect(
        url.includes('/api/users') ||
          url.includes('/login') ||
          url.includes('/oauth') ||
          url.includes('/auth') ||
          url.includes('access_token') ||
          url.includes('/dashboard')
      ).toBeTruthy();
    }
  });

  test('full login flow with credential entry and post-login validation', async ({ page }) => {
    const testEmail = process.env.TEST_USER_EMAIL || 'user@stickerlandia.com';
    console.log(`Testing login with: ${testEmail}`);

    // Perform the full login flow using shared helper
    await performLogin(page);

    // Validate the logged-in state
    await validateLoggedInState(page);

    console.log('Login test passed - user is authenticated and dashboard is displayed');
  });
});
