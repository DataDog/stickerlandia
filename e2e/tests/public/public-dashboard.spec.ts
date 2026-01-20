import { test, expect } from '@playwright/test';
import { PublicDashboardPage } from '../../page-objects/dashboard.page';

test.describe('Public Dashboard', () => {
  let publicDashboard: PublicDashboardPage;

  test.beforeEach(async ({ page }) => {
    publicDashboard = new PublicDashboardPage(page);
    await publicDashboard.goto();
  });

  test('loads without authentication', async () => {
    await publicDashboard.expectToBeVisible();
  });

  test('displays page content', async ({ page }) => {
    // Wait for content to load
    await page.waitForLoadState('networkidle');

    // The page should have some visible content
    const mainContent = page.locator('main, #main, [role="main"]').first();
    await expect(mainContent).toBeVisible();
  });

  test('can navigate back to home', async ({ page }) => {
    // Look for any link that goes to home
    const homeLink = page.getByRole('link', { name: /home|stickerlandia/i }).first();

    if (await homeLink.isVisible()) {
      await homeLink.click();
      await expect(page).toHaveURL('/');
    }
  });

  test('is accessible via direct URL', async ({ page }) => {
    // Navigate directly to public dashboard
    const response = await page.goto('/public-dashboard');
    expect(response?.status()).toBeLessThan(400);
    await expect(page).toHaveURL(/\/public-dashboard/);
  });
});
