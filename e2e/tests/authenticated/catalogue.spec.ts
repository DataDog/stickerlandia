import { test, expect } from '@playwright/test';
import { CataloguePage } from '../../page-objects/catalogue.page';
import { StickerDetailPage } from '../../page-objects/collection.page';

test.describe('Sticker Catalogue', () => {
  test.use({ storageState: '.auth/user.json' });

  let catalogue: CataloguePage;

  test.beforeEach(async ({ page }) => {
    catalogue = new CataloguePage(page);
    await catalogue.goto();
  });

  test('displays page title and description', async ({ page }) => {
    await expect(catalogue.pageTitle).toHaveText('Sticker Catalogue');
    await expect(page.getByText('Browse all available stickers in the collection.')).toBeVisible();
  });

  test('displays stickers from API', async () => {
    await catalogue.waitForStickersLoaded();
    const count = await catalogue.getStickerCount();
    expect(count).toBeGreaterThan(0);
  });

  test('sticker cards show name, image, and description', async () => {
    await catalogue.waitForStickersLoaded();

    const firstCard = catalogue.stickerCards.first();
    await expect(firstCard.locator('h3')).toBeVisible(); // Name
    await expect(firstCard.locator('img')).toBeVisible(); // Image
    await expect(firstCard.locator('p').first()).toBeVisible(); // Description
  });

  test('sticker cards show availability', async () => {
    await catalogue.waitForStickersLoaded();

    const firstCard = catalogue.stickerCards.first();
    // Should show either "X remaining" or "Unlimited available"
    await expect(firstCard.getByText(/remaining|Unlimited available/)).toBeVisible();
  });

  test('clicking sticker navigates to detail page', async ({ page }) => {
    await catalogue.waitForStickersLoaded();
    await catalogue.clickSticker(0);

    await expect(page).toHaveURL(/\/stickers\/[\w-]+/);
  });

  test('is accessible via direct URL', async ({ page }) => {
    const response = await page.goto('/catalogue');
    expect(response?.status()).toBeLessThan(400);
    await catalogue.expectToBeVisible();
  });

  test('shows sidebar navigation', async ({ page }) => {
    await expect(page.getByRole('link', { name: /Catalogue/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /My Collection/i })).toBeVisible();
  });
});

test.describe('Catalogue Pagination', () => {
  test.use({ storageState: '.auth/user.json' });

  let catalogue: CataloguePage;

  test.beforeEach(async ({ page }) => {
    catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();
  });

  test('displays pagination info when stickers exist', async () => {
    const stickerCount = await catalogue.getStickerCount();
    if (stickerCount > 0) {
      await expect(catalogue.pageInfo).toBeVisible();
      await expect(catalogue.showingInfo).toBeVisible();
    }
  });

  test('previous button is disabled on first page', async () => {
    const stickerCount = await catalogue.getStickerCount();
    if (stickerCount > 0) {
      await expect(catalogue.previousButton).toBeDisabled();
    }
  });

  test('can navigate to next page if multiple pages exist', async () => {
    const isNextEnabled = await catalogue.nextButton.isEnabled();

    if (isNextEnabled) {
      await catalogue.goToNextPage();
      await catalogue.waitForStickersLoaded();
      await expect(catalogue.pageInfo).toContainText('Page 2');
      await expect(catalogue.previousButton).toBeEnabled();
    }
  });

  test('can navigate back to previous page', async () => {
    const isNextEnabled = await catalogue.nextButton.isEnabled();

    if (isNextEnabled) {
      await catalogue.goToNextPage();
      await catalogue.waitForStickersLoaded();
      await catalogue.goToPreviousPage();
      await catalogue.waitForStickersLoaded();
      await expect(catalogue.pageInfo).toContainText('Page 1');
    }
  });
});

test.describe('Sticker Detail Page', () => {
  test.use({ storageState: '.auth/user.json' });

  test('displays sticker information', async ({ page }) => {
    // First get a valid sticker ID from the catalogue
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();

    // Click first sticker to navigate to detail
    await catalogue.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();

    // Verify all sections are visible
    await expect(detail.stickerTitle).toBeVisible();
    await expect(detail.stickerId).toBeVisible();
    await expect(detail.stickerImage).toBeVisible();
  });

  test('shows description section', async ({ page }) => {
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();
    await catalogue.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();

    await expect(page.getByRole('heading', { name: 'Description' })).toBeVisible();
  });

  test('shows availability section with status indicator', async ({ page }) => {
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();
    await catalogue.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();

    await expect(page.getByRole('heading', { name: 'Availability' })).toBeVisible();
    await expect(detail.availabilityIndicator).toBeVisible();
  });

  test('shows details section with dates', async ({ page }) => {
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();
    await catalogue.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();

    await expect(page.getByRole('heading', { name: 'Details' })).toBeVisible();
    await expect(page.getByText('Created')).toBeVisible();
    await expect(page.getByText('Last Updated')).toBeVisible();
  });

  test('back link navigates to catalogue', async ({ page }) => {
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();
    await catalogue.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await detail.clickBack();

    await expect(page).toHaveURL(/\/catalogue/);
  });

  test('handles invalid sticker ID gracefully', async ({ page }) => {
    await page.goto('/stickers/non-existent-sticker-xyz');

    // Wait for loading to complete
    await page.waitForLoadState('networkidle');

    // Wait for loading indicator to disappear
    const loadingIndicator = page.getByText('Loading sticker details...');
    await loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {});

    // Should show error message or the back link should be visible
    // The page should have a link back to catalogue (either "Back to Catalogue" or "Return to Catalogue")
    await expect(page.getByRole('link', { name: /Back to Catalogue/i })).toBeVisible();
  });

  test('is accessible via direct URL with valid ID', async ({ page, request }) => {
    // Get a valid sticker ID from API
    const apiResponse = await request.get('/api/stickers/v1');
    const data = await apiResponse.json();
    const stickerId = data.stickers[0]?.stickerId;

    if (stickerId) {
      const response = await page.goto(`/stickers/${stickerId}`);
      expect(response?.status()).toBeLessThan(400);
    }
  });
});
