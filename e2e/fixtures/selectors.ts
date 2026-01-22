/**
 * Shared selectors for common UI elements.
 * Centralizes locator strings to avoid duplication across tests.
 */

// Login form selectors (IdP login page)
export const LOGIN_SELECTORS = {
  emailInput:
    'input[type="email"], input[name="email"], input[name="Email"], input[name="username"], input[id="Email"], input[id="Input_Email"]',
  passwordInput:
    'input[type="password"], input[name="password"], input[name="Password"], input[id="Password"], input[id="Input_Password"]',
  submitButton: 'button[type="submit"], input[type="submit"]',
};

// Registration form selectors
export const REGISTRATION_SELECTORS = {
  firstNameInput:
    'input[name="FirstName"], input[name="Input.FirstName"], input[id="FirstName"], input[id="Input_FirstName"]',
  lastNameInput:
    'input[name="LastName"], input[name="Input.LastName"], input[id="LastName"], input[id="Input_LastName"]',
  emailInput:
    'input[type="email"], input[name="Email"], input[name="Input.Email"], input[id="Email"], input[id="Input_Email"]',
  passwordInput:
    'input[type="password"][name*="Password"]:not([name*="Confirm"]), input[id="Password"], input[id="Input_Password"]',
  confirmPasswordInput:
    'input[type="password"][name*="Confirm"], input[id="ConfirmPassword"], input[id="Input_ConfirmPassword"]',
  submitButton: 'button[type="submit"], input[type="submit"]',
};

/**
 * Waits for token URL to be processed by the app.
 * Used after OAuth redirect when URL contains access_token.
 */
export async function waitForTokenProcessing(page: import('@playwright/test').Page): Promise<void> {
  if (page.url().includes('access_token=')) {
    await page.waitForFunction(() => !window.location.search.includes('access_token'), {
      timeout: 15000,
    });
  }
}

/**
 * Navigates to dashboard if not already there.
 */
export async function ensureOnDashboard(page: import('@playwright/test').Page): Promise<void> {
  if (!page.url().includes('/dashboard')) {
    await page.goto('/dashboard');
  }
  await page.waitForLoadState('networkidle');
}
