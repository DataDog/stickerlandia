import { test as setup, expect } from '@playwright/test';
import { LOGIN_SELECTORS, waitForTokenProcessing } from '../../fixtures/selectors';

const adminAuthFile = '.auth/admin.json';

/**
 * Authenticates as an admin user and saves auth state.
 * Uses the seeded admin account (admin@stickerlandia.com).
 */
setup('authenticate as admin', async ({ page }) => {
  const baseUrl = process.env.BASE_URL || 'http://localhost:8080';
  const adminEmail = process.env.TEST_ADMIN_EMAIL || 'admin@stickerlandia.com';
  const adminPassword = process.env.TEST_ADMIN_PASSWORD || 'Admin2025!';

  console.log(`[adminSetup] Authenticating as admin: ${adminEmail}`);

  await page.goto(baseUrl);
  await page.waitForLoadState('networkidle');

  const signInButton = page.getByRole('button', { name: /sign in/i });
  await expect(signInButton).toBeVisible({ timeout: 10000 });
  await signInButton.click();
  console.log(`[adminSetup] Clicked Sign In button`);

  await page.waitForURL((url) => url.href !== baseUrl && url.href !== `${baseUrl}/`, {
    timeout: 30000,
  });
  await page.waitForLoadState('networkidle');
  await page.waitForLoadState('domcontentloaded');

  const emailInput = page.locator(LOGIN_SELECTORS.emailInput).first();

  try {
    await expect(emailInput).toBeVisible({ timeout: 20000 });
  } catch {
    const currentUrl = page.url();
    console.log(`[adminSetup] Email input not found. Current URL: ${currentUrl}`);
    if (currentUrl.includes('access_token=') || currentUrl.includes('/dashboard')) {
      if (currentUrl.includes('access_token=')) {
        await waitForTokenProcessing(page);
      }
      if (!page.url().includes('/dashboard')) {
        await page.goto(`${baseUrl}/dashboard`);
      }
      await page.waitForLoadState('networkidle');
      await page.context().storageState({ path: adminAuthFile });
      return;
    }
    throw new Error(`Login form did not appear within timeout. Current URL: ${page.url()}`);
  }

  await emailInput.fill(adminEmail);
  const passwordInput = page.locator(LOGIN_SELECTORS.passwordInput).first();
  await expect(passwordInput).toBeVisible({ timeout: 5000 });
  await passwordInput.fill(adminPassword);

  const submitButton = page.locator(LOGIN_SELECTORS.submitButton).first();
  await expect(submitButton).toBeVisible({ timeout: 5000 });
  await submitButton.click();
  console.log(`[adminSetup] Submitted admin credentials`);

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

  if (page.url().includes('access_token=')) {
    await waitForTokenProcessing(page);
  }

  await page.waitForTimeout(1000);

  if (!page.url().includes('/dashboard')) {
    await page.goto(`${baseUrl}/dashboard`);
  }

  await page.waitForLoadState('networkidle');
  await page.getByText(/Welcome Back/i).waitFor({ state: 'visible', timeout: 15000 });
  console.log(`[adminSetup] Admin login complete`);

  // Copy sessionStorage auth token to localStorage so storageState preserves it
  // (storageState saves cookies + localStorage, but NOT sessionStorage)
  await page.evaluate(() => {
    const token = sessionStorage.getItem('auth_token');
    if (token) {
      localStorage.setItem('auth_token_backup', token);
    }
  });

  await page.context().storageState({ path: adminAuthFile });
  console.log(`[adminSetup] Admin auth state saved to ${adminAuthFile}`);
});
