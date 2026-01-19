import { test, expect } from '@playwright/test';

test.describe('User Dashboard', () => {
  test.use({ storageState: '.auth/user.json' });

  test('authenticated user can access dashboard', async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.locator('main#main')).toBeAttached();
  });
});
