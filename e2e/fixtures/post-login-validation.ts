import { Page, expect, Response } from '@playwright/test';

export interface ApiCall {
  url: string;
  status: number;
  bytes: number;
}

export interface ValidationResult {
  userName: string;
  stickerCount: number;
  apiCalls: {
    stickerAward: ApiCall | null;
    stickerCatalogue: ApiCall[];
  };
}

/**
 * Shared validation for post-login state.
 * Used by both login and registration tests to verify successful authentication.
 *
 * Validates:
 * - User's name is displayed (not just "Welcome Back")
 * - Sticker count is >= 0
 * - API calls to sticker-award and sticker-catalogue succeeded with data
 *
 * Note: The assertions in this fixture are sanity check assertions - they verify
 * that the shared post-login state is valid before individual tests run their
 * specific assertions. If these fail, it indicates a fundamental auth or
 * dashboard loading issue rather than a test-specific failure.
 */
export async function validateLoggedInState(page: Page): Promise<ValidationResult> {
  const apiCalls: Response[] = [];

  // Set up network interception for API calls
  page.on('response', (response) => {
    const url = response.url();
    if (url.includes('/api/awards/') || url.includes('/api/stickers/')) {
      apiCalls.push(response);
    }
  });

  // 1. Verify we're on the dashboard
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 15000 });

  // Reload to capture API calls (they may have already happened)
  await page.reload();
  await page.waitForLoadState('networkidle');

  // Wait for dashboard to fully render after reload
  // Wait for actual content rather than container (which may be hidden during loading)
  await page.getByText(/Welcome Back/i).waitFor({ state: 'visible', timeout: 20000 });

  // 2. Verify welcome message with user's name is displayed
  // Format: "Welcome Back, {name}!"
  const welcomeMessage = page.getByText(/Welcome Back,\s+\w+/i);
  await expect(welcomeMessage).toBeVisible({ timeout: 15000 });

  // Extract the user's name from the welcome message
  const welcomeText = await welcomeMessage.textContent();
  const nameMatch = welcomeText?.match(/Welcome Back,\s+(\w+)/i);
  const userName = nameMatch ? nameMatch[1] : '';
  expect(userName.length).toBeGreaterThan(0);
  console.log(`[validateLoggedInState] User name: ${userName}`);

  // 3. Verify "Sticker Collector" text is visible (sidebar user info)
  const stickerCollectorText = page.getByText('Sticker Collector');
  await expect(stickerCollectorText).toBeVisible({ timeout: 10000 });

  // 4. Verify Sign Out is visible (indicates authenticated state)
  const signOut = page.getByText('Sign Out');
  await expect(signOut).toBeVisible({ timeout: 10000 });

  // 5. Verify user stats are displayed and extract sticker count
  // Format: "Total Stickers" followed by a number
  const totalStickersLabel = page.getByText('Total Stickers');
  await expect(totalStickersLabel).toBeVisible({ timeout: 10000 });

  // Get the sticker count - it's in a sibling element or nearby
  // The page shows "Total Stickers" with the count in an adjacent element
  const getStickerCount = async (): Promise<number> => {
    const statsSection = page.locator('text=Total Stickers').locator('..');
    const statsText = await statsSection.textContent();
    const countMatch = statsText?.match(/Total Stickers\s*(\d+)/i);
    return countMatch ? parseInt(countMatch[1], 10) : -1;
  };

  let stickerCount = await getStickerCount();
  console.log(`[validateLoggedInState] Sticker count: ${stickerCount}`);

  // On AWS, sticker assignment can take a few seconds after registration
  // If no stickers yet, wait and retry once
  if (stickerCount === 0) {
    console.log(`[validateLoggedInState] No stickers yet, waiting 30s for assignment...`);
    await page.waitForTimeout(30000);
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('main#main', { state: 'visible', timeout: 15000 });
    stickerCount = await getStickerCount();
    console.log(`[validateLoggedInState] Sticker count after retry: ${stickerCount}`);
  }

  expect(stickerCount).toBeGreaterThanOrEqual(0);

  // 6. Verify main content area is present
  const mainContent = page.locator('main#main');
  await expect(mainContent).toBeVisible({ timeout: 10000 });

  // 7. Validate API calls to sticker-award and sticker-catalogue
  // Wait a moment for any pending requests to complete
  await page.waitForTimeout(500);

  let stickerAwardCall: ApiCall | null = null;
  const stickerCatalogueCalls: ApiCall[] = [];

  for (const response of apiCalls) {
    const url = response.url();
    const status = response.status();

    // Get response body size
    let bytes = 0;
    try {
      const body = await response.body();
      bytes = body.length;
    } catch {
      // Response body may not be available for some responses
      bytes = 0;
    }

    if (url.includes('/api/awards/v1/assignments/')) {
      stickerAwardCall = { url, status, bytes };
      console.log(`[validateLoggedInState] sticker-award API: ${status}, ${bytes} bytes`);
    } else if (url.includes('/api/stickers/v1/')) {
      stickerCatalogueCalls.push({ url, status, bytes });
      console.log(`[validateLoggedInState] sticker-catalogue API: ${status}, ${bytes} bytes`);
    }
  }

  // Validate sticker-award API call
  expect(stickerAwardCall, 'Expected sticker-award API call').not.toBeNull();
  expect(stickerAwardCall!.status).toBe(200);
  expect(stickerAwardCall!.bytes).toBeGreaterThan(0);

  // Validate sticker-catalogue API calls (sticker images) if any were made
  // Note: Images may be cached, so we only validate calls that were actually made
  for (const call of stickerCatalogueCalls) {
    expect(call.status).toBe(200);
    expect(call.bytes).toBeGreaterThan(0);
  }
  if (stickerCatalogueCalls.length > 0) {
    console.log(`[validateLoggedInState] Validated ${stickerCatalogueCalls.length} sticker-catalogue calls`);
  } else {
    console.log(`[validateLoggedInState] No sticker-catalogue calls captured (images may be cached)`);
  }

  return {
    userName,
    stickerCount,
    apiCalls: {
      stickerAward: stickerAwardCall,
      stickerCatalogue: stickerCatalogueCalls,
    },
  };
}

/**
 * Extended validation that also checks specific user data loaded.
 * Sanity check assertions - verifies user-specific data is populated.
 */
export async function validateLoggedInStateWithUserData(
  page: Page,
  expectedName?: string
): Promise<ValidationResult> {
  // First do the standard validation
  const result = await validateLoggedInState(page);

  // If expected name provided, verify it matches
  if (expectedName) {
    expect(result.userName.toLowerCase()).toContain(expectedName.toLowerCase());
  }

  // Verify member stats are populated (not just labels)
  const memberSince = page.getByText(/Member Since/i);
  await expect(memberSince).toBeVisible({ timeout: 10000 });

  return result;
}

/**
 * Generates a random email for registration tests.
 */
export function generateRandomEmail(): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  return `test-${timestamp}-${random}@stickerlandia-test.com`;
}

/**
 * Generates a random secure password for registration tests.
 */
export function generateRandomPassword(): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%';
  let password = 'Test'; // Start with capital letter
  for (let i = 0; i < 12; i++) {
    password += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  password += '1!'; // Ensure number and special char
  return password;
}
