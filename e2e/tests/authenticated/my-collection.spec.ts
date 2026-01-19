import { test, expect } from '@playwright/test';

test.describe('My Collection', () => {
  test.use({ storageState: '.auth/user.json' });

  test('authenticated user can access collection page', async ({ page }) => {
    await page.goto('/collection');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/collection/);
    await expect(page.locator('main#main')).toBeAttached();
  });
});
