import { type Locator, type Page, expect } from '@playwright/test';

export class CollectionPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly stickerGrid: Locator;
  readonly stickerCards: Locator;
  readonly emptyState: Locator;
  readonly filterControls: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h1, h2').first();
    this.stickerGrid = page.locator('[data-testid="sticker-grid"]');
    this.stickerCards = page.locator('[data-testid="sticker-card"]');
    this.emptyState = page.locator('[data-testid="empty-state"]');
    this.filterControls = page.locator('[data-testid="filter-controls"]');
  }

  async goto() {
    await this.page.goto('/collection');
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/collection/);
  }

  async clickSticker(index: number) {
    await this.stickerCards.nth(index).click();
  }

  async expectStickersLoaded() {
    // Either stickers are shown or empty state
    const hasStickers = await this.stickerCards.count() > 0;
    const hasEmptyState = await this.emptyState.isVisible().catch(() => false);
    expect(hasStickers || hasEmptyState).toBeTruthy();
  }
}

export class StickerDetailPage {
  readonly page: Page;
  readonly stickerImage: Locator;
  readonly stickerTitle: Locator;
  readonly stickerId: Locator;
  readonly descriptionSection: Locator;
  readonly stickerDescription: Locator;
  readonly availabilitySection: Locator;
  readonly availabilityStatus: Locator;
  readonly availabilityIndicator: Locator;
  readonly detailsSection: Locator;
  readonly createdDate: Locator;
  readonly updatedDate: Locator;
  readonly backLink: Locator;
  readonly loadingIndicator: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.stickerImage = page.locator('.aspect-square img').first();
    this.stickerTitle = page.locator('h1');
    this.stickerId = page.getByText(/^ID:/);
    this.descriptionSection = page.locator('h2:has-text("Description")').locator('..');
    this.stickerDescription = page.locator('h2:has-text("Description") + p');
    this.availabilitySection = page.locator('h2:has-text("Availability")').locator('..');
    this.availabilityStatus = page.locator('h2:has-text("Availability")').locator('..').locator('span').last();
    this.availabilityIndicator = page.locator('.rounded-full.w-3.h-3');
    this.detailsSection = page.locator('h2:has-text("Details")').locator('..');
    this.createdDate = page.locator('dt:has-text("Created") + dd');
    this.updatedDate = page.locator('dt:has-text("Last Updated") + dd');
    this.backLink = page.getByRole('link', { name: /Back to Catalogue/i });
    this.loadingIndicator = page.getByText('Loading sticker details...');
    this.errorMessage = page.locator('.text-red-500');
  }

  async goto(id: string) {
    await this.page.goto(`/stickers/${id}`);
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/stickers\/[\w-]+/);
  }

  async waitForLoaded() {
    await this.loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
  }

  async clickBack() {
    await this.backLink.click();
  }
}
