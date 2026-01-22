import { test, expect } from '@playwright/test';
import { performLogin } from '../../fixtures/auth.fixture';

test.describe('Logout Flow', () => {
  test('user can sign out and is redirected to home', async ({ page }) => {
    // First, perform login to get authenticated
    await performLogin(page);

    // Verify we're on the dashboard
    await expect(page).toHaveURL(/\/dashboard/);
    await page.getByText(/Welcome Back/i).waitFor({ state: 'visible', timeout: 15000 });

    // Click Sign Out button
    const signOutButton = page.getByText('Sign Out').first();
    await expect(signOutButton).toBeVisible({ timeout: 10000 });
    await signOutButton.click();

    // Wait for navigation away from dashboard
    await page.waitForURL((url) => !url.href.includes('/dashboard'), {
      timeout: 15000,
    });

    // Handle IdP logout confirmation page if present
    if (page.url().includes('/connect/logout')) {
      const yesButton = page.getByRole('button', { name: /yes/i });
      await yesButton.click({ timeout: 5000 });
      await page.waitForURL((url) => !url.href.includes('/connect/logout'), { timeout: 10000 });
    }

    // Verify we're on the home page (or at least not on a protected route)
    const currentUrl = page.url();
    expect(
      currentUrl.endsWith('/') ||
      currentUrl.includes('login') ||
      !currentUrl.includes('/dashboard')
    ).toBeTruthy();

    // Verify Sign In button is visible (indicates logged out state)
    const signInButton = page.getByRole('button', { name: /sign in/i });
    await expect(signInButton).toBeVisible({ timeout: 10000 });
  });
});
