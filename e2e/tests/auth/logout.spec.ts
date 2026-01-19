import { test, expect } from '@playwright/test';

test.describe('Logout Flow', () => {
  test.use({ storageState: '.auth/user.json' });

  test.skip('user can sign out and is redirected to home', async ({ page }) => {
    // Start on dashboard (authenticated via storageState)
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/dashboard/);

    // Wait for dashboard to fully render
    await page.getByText(/Welcome Back/i).waitFor({ state: 'visible', timeout: 15000 });
    console.log(`Dashboard loaded, looking for Sign Out`);

    // Find and click Sign Out
    const signOutButton = page.getByText('Sign Out');
    await expect(signOutButton).toBeVisible({ timeout: 10000 });
    await signOutButton.click();

    // Should be redirected away from dashboard (to home or login)
    await page.waitForURL((url) => !url.href.includes('/dashboard'), {
      timeout: 15000,
    });

    console.log(`After sign out, URL is: ${page.url()}`);

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
