import { test, expect } from '@playwright/test';
import {
  validateLoggedInState,
  generateRandomEmail,
  generateRandomPassword,
} from '../../fixtures/post-login-validation';
import {
  LOGIN_SELECTORS,
  REGISTRATION_SELECTORS,
  waitForTokenProcessing,
  ensureOnDashboard,
} from '../../fixtures/selectors';

test.describe('Registration Flow', () => {
  test('new user can register and access dashboard', async ({ page }) => {
    const baseUrl = process.env.BASE_URL || 'http://localhost:8080';
    const testEmail = generateRandomEmail();
    const testPassword = generateRandomPassword();
    const firstName = 'Test';
    const lastName = 'User';

    console.log(`Registering new user: ${testEmail}`);

    // Navigate to the app
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Click to start the auth flow
    const startButton = page.getByRole('button', { name: /start collecting/i });
    await expect(startButton).toBeVisible({ timeout: 10000 });
    await startButton.click();

    // Wait for navigation away from home page
    await page.waitForURL((url) => url.href !== baseUrl && url.href !== `${baseUrl}/`, {
      timeout: 30000,
    });

    console.log(`After clicking Start Collecting, URL is: ${page.url()}`);

    // Wait for the login page to fully render by waiting for the email input
    const loginEmailInput = page.locator(LOGIN_SELECTORS.emailInput).first();
    await expect(loginEmailInput).toBeVisible({ timeout: 15000 });
    console.log('Login page loaded - email input visible');

    // Now look for the registration link - it should be present
    const registerLink = page.getByRole('link', { name: /register|sign up|create account/i });
    await expect(registerLink).toBeVisible({ timeout: 10000 });
    console.log('Found register link, clicking it');
    await registerLink.click();

    // Wait for navigation to registration page
    await page.waitForURL(/register/i, { timeout: 15000 });
    console.log(`After clicking register link, URL is: ${page.url()}`);

    // Wait for registration form to be visible
    const firstNameInput = page.locator(REGISTRATION_SELECTORS.firstNameInput).first();
    const lastNameInput = page.locator(REGISTRATION_SELECTORS.lastNameInput).first();
    const emailInput = page.locator(REGISTRATION_SELECTORS.emailInput).first();
    const passwordInput = page.locator(REGISTRATION_SELECTORS.passwordInput).first();
    const confirmPasswordInput = page.locator(REGISTRATION_SELECTORS.confirmPasswordInput).first();
    const submitButton = page.locator(REGISTRATION_SELECTORS.submitButton).first();

    // Wait for form to be ready
    await expect(emailInput).toBeVisible({ timeout: 20000 });

    // Fill in registration form
    if (await firstNameInput.isVisible().catch(() => false)) {
      await firstNameInput.fill(firstName);
    }
    if (await lastNameInput.isVisible().catch(() => false)) {
      await lastNameInput.fill(lastName);
    }
    await emailInput.fill(testEmail);
    await passwordInput.fill(testPassword);
    if (await confirmPasswordInput.isVisible().catch(() => false)) {
      await confirmPasswordInput.fill(testPassword);
    }

    // Submit registration form
    await expect(submitButton).toBeVisible({ timeout: 5000 });
    await submitButton.click();

    // Wait for redirect after successful registration
    await page.waitForURL(
      (url) => {
        const href = url.href;
        return (
          href.includes('access_token=') ||
          href.includes('/dashboard') ||
          (href.includes('localhost:8080') && !href.includes('/api/users'))
        );
      },
      { timeout: 30000 }
    );

    // Handle token URL and navigate to dashboard
    await waitForTokenProcessing(page);
    await page.waitForTimeout(1000);
    await ensureOnDashboard(page);

    // Validate logged-in state using shared validation
    await validateLoggedInState(page);

    console.log(`Registration test passed - new user ${testEmail} is authenticated`);
  });
});
