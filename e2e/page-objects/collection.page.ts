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
  readonly stickerDescription: Locator;
  readonly backButton: Locator;
  readonly shareButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.stickerImage = page.locator('[data-testid="sticker-image"]');
    this.stickerTitle = page.locator('h1, h2').first();
    this.stickerDescription = page.locator('[data-testid="sticker-description"]');
    this.backButton = page.getByRole('button', { name: /back/i });
    this.shareButton = page.getByRole('button', { name: /share/i });
  }

  async goto(id: string) {
    await this.page.goto(`/stickers/${id}`);
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/stickers\/\w+/);
  }

  async clickBack() {
    await this.backButton.click();
  }
}
