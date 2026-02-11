import { test as setup, expect } from '@playwright/test';
import { performLogin } from '../../fixtures/auth.fixture';

const authFile = '.auth/user.json';

setup('authenticate', async ({ page }) => {
  // Log the email being used (password comes from env or defaults in performLogin)
  const testEmail = process.env.TEST_USER_EMAIL || 'user@stickerlandia.com';
  console.log(`Authenticating as: ${testEmail}`);

  // Perform login
  await performLogin(page);

  // Verify we're on the dashboard
  await expect(page).toHaveURL(/\/dashboard/);

  // Copy sessionStorage auth token to localStorage so storageState preserves it
  // (storageState saves cookies + localStorage, but NOT sessionStorage)
  await page.evaluate(() => {
    const token = sessionStorage.getItem('auth_token');
    if (token) {
      localStorage.setItem('auth_token_backup', token);
    }
  });

  // Save authentication state
  await page.context().storageState({ path: authFile });
  console.log('Authentication state saved successfully');
});
