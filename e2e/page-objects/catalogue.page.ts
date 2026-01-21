import { type Locator, type Page, expect } from '@playwright/test';

export class CataloguePage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly pageDescription: Locator;
  readonly stickerGrid: Locator;
  readonly stickerCards: Locator;
  readonly emptyState: Locator;
  readonly loadingIndicator: Locator;
  readonly errorMessage: Locator;

  // Pagination
  readonly paginationContainer: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly pageInfo: Locator;
  readonly showingInfo: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h1');
    this.pageDescription = page.locator('p.text-gray-600').first();
    this.stickerGrid = page.locator('.grid').first();
    this.stickerCards = page.locator('a.landing-card');
    this.emptyState = page.getByText('No stickers found in the catalogue.');
    this.loadingIndicator = page.getByText('Loading stickers...');
    this.errorMessage = page.locator('.text-red-500');

    // Pagination elements
    this.paginationContainer = page.locator('.border-t.border-gray-200');
    this.previousButton = page.getByRole('button', { name: 'Previous' });
    this.nextButton = page.getByRole('button', { name: 'Next' });
    this.pageInfo = page.getByText(/Page \d+ of \d+/);
    this.showingInfo = page.getByText(/Showing \d+ to \d+ of \d+ stickers/);
  }

  async goto() {
    await this.page.goto('/catalogue');
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/catalogue/);
  }

  async waitForStickersLoaded() {
    await this.loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
  }

  async clickSticker(index: number) {
    await this.stickerCards.nth(index).click();
  }

  async getStickerCount(): Promise<number> {
    return await this.stickerCards.count();
  }

  async goToNextPage() {
    await this.nextButton.click();
  }

  async goToPreviousPage() {
    await this.previousButton.click();
  }
}
