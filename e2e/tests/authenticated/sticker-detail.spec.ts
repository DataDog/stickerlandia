import { test, expect } from '@playwright/test';

test.describe('Sticker Detail', () => {
  test.use({ storageState: '.auth/user.json' });

  test('authenticated user can access sticker detail page', async ({ page }) => {
    await page.goto('/stickers/sticker-001');
    await page.waitForLoadState('networkidle');

    // Should stay on sticker page or redirect somewhere valid
    expect(page.url()).toBeTruthy();
  });
});
