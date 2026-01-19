import { test as base, expect, type Page } from '@playwright/test';
import { LOGIN_SELECTORS, waitForTokenProcessing } from './selectors';

// Extend the base test with custom fixtures
export const test = base.extend<{
  authenticatedPage: Page;
}>({
  authenticatedPage: async ({ browser }, use) => {
    // Create a new context with stored auth state
    const context = await browser.newContext({
      storageState: '.auth/user.json',
    });
    const page = await context.newPage();
    await use(page);
    await context.close();
  },
});

export { expect };

/**
 * Helper function to perform login via the OIDC flow.
 * Used by auth setup and login tests.
 *
 * Note: The assertions in this function are sanity check assertions - they verify
 * that the login flow elements are present and functional. If these fail, it
 * indicates an infrastructure or IdP issue rather than a test-specific failure.
 */
export async function performLogin(page: Page): Promise<void> {
  const baseUrl = process.env.BASE_URL || 'http://localhost:8080';
  const testEmail = process.env.TEST_USER_EMAIL || 'user@stickerlandia.com';
  const testPassword = process.env.TEST_USER_PASSWORD || 'Stickerlandia2025!';

  console.log(`[performLogin] Starting login flow to ${baseUrl}`);

  // Navigate to home page
  await page.goto(baseUrl);
  await page.waitForLoadState('networkidle');
  console.log(`[performLogin] Home page loaded`);

  // Click the "Sign In" button in the header to initiate login
  const signInButton = page.getByRole('button', { name: /sign in/i });
  await expect(signInButton).toBeVisible({ timeout: 10000 });
  await expect(signInButton).toBeEnabled({ timeout: 5000 });
  await signInButton.click();
  console.log(`[performLogin] Clicked Sign In button`);

  // Wait for navigation away from home page
  await page.waitForURL((url) => url.href !== baseUrl && url.href !== `${baseUrl}/`, {
    timeout: 30000,
  });
  console.log(`[performLogin] Navigated to: ${page.url()}`);

  // Wait for the page to fully load (the login form is server-rendered)
  await page.waitForLoadState('networkidle');
  await page.waitForLoadState('domcontentloaded');
  console.log(`[performLogin] Page loaded, looking for email input`);

  // Define the email input locator
  const emailInput = page.locator(LOGIN_SELECTORS.emailInput).first();

  // Wait for login form with longer timeout - the server-rendered page can be slow
  try {
    await expect(emailInput).toBeVisible({ timeout: 20000 });
    console.log(`[performLogin] Email input visible`);
  } catch {
    // Check if we were redirected back with a token (already authenticated)
    const currentUrl = page.url();
    console.log(`[performLogin] Email input not found. Current URL: ${currentUrl}`);
    if (currentUrl.includes('access_token=') || currentUrl.includes('/dashboard')) {
      console.log(`[performLogin] Already authenticated, skipping login`);
      // Already authenticated, skip login
      if (currentUrl.includes('access_token=')) {
        await page.waitForFunction(
          () => !window.location.search.includes('access_token'),
          { timeout: 15000 }
        );
      }
      if (!page.url().includes('/dashboard')) {
        await page.goto(`${baseUrl}/dashboard`);
      }
      await page.waitForLoadState('networkidle');
      await page.waitForSelector('main#main', { timeout: 10000 });
      return;
    }
    // Re-throw with more context
    throw new Error(`Login form did not appear within timeout. Current URL: ${page.url()}`);
  }

  // Fill in credentials
  console.log(`[performLogin] Filling credentials for ${testEmail}`);
  await emailInput.fill(testEmail);

  const passwordInput = page.locator(LOGIN_SELECTORS.passwordInput).first();
  await expect(passwordInput).toBeVisible({ timeout: 5000 });
  await passwordInput.fill(testPassword);

  // Click submit
  const submitButton = page.locator(LOGIN_SELECTORS.submitButton).first();
  await expect(submitButton).toBeVisible({ timeout: 5000 });
  await submitButton.click();
  console.log(`[performLogin] Clicked submit button`);

  // Wait for redirect back to the app - either with token or to dashboard
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
  console.log(`[performLogin] Redirected to: ${page.url()}`);

  // Handle token URL processing
  if (page.url().includes('access_token=')) {
    console.log(`[performLogin] Waiting for token to be processed`);
    await waitForTokenProcessing(page);
    console.log(`[performLogin] Token processed, URL now: ${page.url()}`);
  }

  // Give the app time to process auth state
  await page.waitForTimeout(1000);

  // Navigate to dashboard if not already there
  if (!page.url().includes('/dashboard')) {
    console.log(`[performLogin] Navigating to dashboard`);
    await page.goto(`${baseUrl}/dashboard`);
  }

  await page.waitForLoadState('networkidle');
  console.log(`[performLogin] On dashboard, waiting for content`);

  // Wait for actual dashboard content (not just the hidden main element)
  // The dashboard shows "Welcome Back" when user data is loaded
  await page.getByText(/Welcome Back/i).waitFor({ state: 'visible', timeout: 15000 });
  console.log(`[performLogin] Login complete - dashboard content visible`);
}

// Helper to check if user is logged in
export async function isLoggedIn(page: Page): Promise<boolean> {
  try {
    await page.goto('/dashboard');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
    return true;
  } catch {
    return false;
  }
}
