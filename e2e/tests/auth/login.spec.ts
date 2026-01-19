import { test, expect } from '@playwright/test';
import { LandingPage } from '../../page-objects/landing.page';
import { validateLoggedInState } from '../../fixtures/post-login-validation';

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
    const emailInput = page.locator(
      'input[type="email"], input[name="email"], input[name="Email"], input[name="username"], input[id="Email"], input[id="Input_Email"]'
    ).first();

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
    const baseUrl = process.env.BASE_URL || 'http://localhost:8080';
    const testEmail = process.env.TEST_USER_EMAIL || 'user@stickerlandia.com';
    const testPassword = process.env.TEST_USER_PASSWORD || 'Stickerlandia2025!';

    console.log(`Testing login with: ${testEmail}`);

    // Start at landing page
    const landingPage = new LandingPage(page);
    await landingPage.goto();

    // Initiate login
    await landingPage.clickStartCollecting();

    // Wait for navigation away from home page
    await page.waitForURL((url) => url.href !== baseUrl && url.href !== `${baseUrl}/`, {
      timeout: 30000,
    });

    // Wait for the page to fully load
    await page.waitForLoadState('networkidle');
    await page.waitForLoadState('domcontentloaded');

    // Define the email input locator
    const emailInput = page.locator(
      'input[type="email"], input[name="email"], input[name="Email"], input[name="username"], input[id="Email"], input[id="Input_Email"]'
    ).first();

    // Wait for login page to fully load
    await expect(emailInput).toBeVisible({ timeout: 20000 });

    // Fill credentials
    await emailInput.fill(testEmail);

    const passwordInput = page.locator(
      'input[type="password"], input[name="password"], input[name="Password"], input[id="Password"], input[id="Input_Password"]'
    ).first();
    await expect(passwordInput).toBeVisible({ timeout: 5000 });
    await passwordInput.fill(testPassword);

    // Find and click submit button
    const submitButton = page.locator('button[type="submit"], input[type="submit"]').first();
    await expect(submitButton).toBeVisible({ timeout: 5000 });
    await submitButton.click();

    // Wait for redirect back to app - either dashboard or token URL
    await page.waitForURL(
      (url) => {
        const href = url.href;
        return (
          href.includes('access_token=') ||
          href.includes('/dashboard') ||
          (href.includes('localhost:8080') && !href.includes('/api/users'))
        );
      },
      { timeout: 30000 }
    );

    // If we landed on a token URL, wait for app to process it
    if (page.url().includes('access_token=')) {
      await page.waitForFunction(
        () => !window.location.search.includes('access_token'),
        { timeout: 15000 }
      );
    }

    // Give the app time to process auth state
    await page.waitForTimeout(1000);

    // If we're not on dashboard, navigate there
    if (!page.url().includes('/dashboard')) {
      await page.goto('/dashboard');
    }

    // Now validate the logged-in state using shared validation
    await validateLoggedInState(page);

    console.log('Login test passed - user is authenticated and dashboard is displayed');
  });
});
