import { test, expect } from '@playwright/test';
import {
  validateLoggedInState,
  generateRandomEmail,
  generateRandomPassword,
} from '../../fixtures/post-login-validation';

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

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');
    await page.waitForLoadState('domcontentloaded');

    // Debug: log current URL after navigation
    console.log(`After clicking Start Collecting, URL is: ${page.url()}`);

    // Look for a registration link on the login page
    const registerLink = page.getByRole('link', { name: /register|sign up|create account/i });

    if (await registerLink.isVisible({ timeout: 5000 }).catch(() => false)) {
      console.log('Found register link, clicking it');
      await registerLink.click();
      await page.waitForLoadState('networkidle');
      console.log(`After clicking register link, URL is: ${page.url()}`);
    } else {
      // The login page should have a register link - if not found, log error
      console.log(`Register link not found. Current URL: ${page.url()}`);
      console.log(`Page title: ${await page.title()}`);
      // Take a screenshot for debugging
      throw new Error(`Could not find registration link on login page. URL: ${page.url()}`);
    }

    // Wait for registration form to be visible
    // The form has: First Name, Last Name, Email, Password, Confirm Password
    const firstNameInput = page
      .locator(
        'input[name="FirstName"], input[name="Input.FirstName"], input[id="FirstName"], input[id="Input_FirstName"]'
      )
      .first();
    const lastNameInput = page
      .locator(
        'input[name="LastName"], input[name="Input.LastName"], input[id="LastName"], input[id="Input_LastName"]'
      )
      .first();
    const emailInput = page
      .locator(
        'input[type="email"], input[name="Email"], input[name="Input.Email"], input[id="Email"], input[id="Input_Email"]'
      )
      .first();
    const passwordInput = page
      .locator(
        'input[type="password"][name*="Password"]:not([name*="Confirm"]), input[id="Password"], input[id="Input_Password"]'
      )
      .first();
    const confirmPasswordInput = page
      .locator(
        'input[type="password"][name*="Confirm"], input[id="ConfirmPassword"], input[id="Input_ConfirmPassword"]'
      )
      .first();

    // Wait for form to be ready with longer timeout
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
    const submitButton = page.locator('button[type="submit"], input[type="submit"]').first();
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

    // If we got a token URL, wait for app to process it
    if (page.url().includes('access_token=')) {
      await page.waitForFunction(
        () => !window.location.search.includes('access_token'),
        { timeout: 15000 }
      );
    }

    // Give the app time to process auth state
    await page.waitForTimeout(1000);

    // Navigate to dashboard if not already there
    if (!page.url().includes('/dashboard')) {
      await page.goto('/dashboard');
    }

    // Validate logged-in state using shared validation
    await validateLoggedInState(page);

    console.log(`Registration test passed - new user ${testEmail} is authenticated`);
  });
});
