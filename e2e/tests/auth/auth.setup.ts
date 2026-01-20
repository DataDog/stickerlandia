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

  // Save authentication state
  await page.context().storageState({ path: authFile });
  console.log('Authentication state saved successfully');
});
