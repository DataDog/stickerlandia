import { test, expect } from '@playwright/test';
import { restoreSessionToken } from '../../fixtures/auth.fixture';

test.describe('Print Station - Standard User Visibility', () => {
  test('does not see Print Station in sidebar navigation', async ({ page }) => {
    // Restore the user JWT so the frontend can read the "user" role
    await restoreSessionToken(page);

    // Navigate to catalogue (a page with sidebar) to check nav items
    await page.goto('/catalogue');
    await page.waitForLoadState('networkidle');

    // Verify sidebar is present first (Catalogue link should be visible)
    await expect(page.getByRole('link', { name: /Catalogue/i })).toBeVisible({ timeout: 10000 });

    // Standard user should NOT see Print Station
    const printStationLink = page.getByRole('link', { name: 'Print Station' });
    await expect(printStationLink).not.toBeVisible();
  });
});
