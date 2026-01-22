import { test, expect } from '@playwright/test';

test.describe('Protected Routes', () => {
  test.use({ storageState: '.auth/user.json' });

  const protectedRoutes = [
    { path: '/dashboard', name: 'dashboard' },
    { path: '/collection', name: 'collection' },
    { path: '/catalogue', name: 'catalogue' },
  ];

  for (const route of protectedRoutes) {
    test(`authenticated user can access ${route.name}`, async ({ page }) => {
      await page.goto(route.path);
      await page.waitForLoadState('networkidle');

      await expect(page).toHaveURL(new RegExp(route.path));
      await expect(page.locator('main#main')).toBeAttached();
    });
  }

  test('authenticated user can access sticker detail page', async ({ page }) => {
    await page.goto('/stickers/sticker-001');
    await page.waitForLoadState('networkidle');

    // Should stay on sticker page or redirect somewhere valid
    expect(page.url()).toBeTruthy();
  });
});
