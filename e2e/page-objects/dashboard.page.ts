import { type Locator, type Page, expect } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly userProfile: Locator;
  readonly stickerGrid: Locator;
  readonly sidebar: Locator;
  readonly collectionLink: Locator;
  readonly logoutButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h1, h2').first();
    this.userProfile = page.locator('[data-testid="user-profile"]');
    this.stickerGrid = page.locator('[data-testid="sticker-grid"]');
    this.sidebar = page.locator('[data-testid="sidebar"]');
    this.collectionLink = page.getByRole('link', { name: /collection/i });
    this.logoutButton = page.getByRole('button', { name: /sign out|logout/i });
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/dashboard/);
  }

  async clickCollection() {
    await this.collectionLink.click();
  }

  async clickLogout() {
    await this.logoutButton.click();
  }

  async expectUserAuthenticated() {
    // After successful auth, user should be on dashboard
    await expect(this.page).toHaveURL(/\/dashboard/);
  }
}

export class PublicDashboardPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly stickerList: Locator;
  readonly backToHomeLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h1, h2').first();
    this.stickerList = page.locator('[data-testid="sticker-list"]');
    this.backToHomeLink = page.getByRole('link', { name: /home|back/i });
  }

  async goto() {
    await this.page.goto('/public-dashboard');
  }

  async expectToBeVisible() {
    await expect(this.page).toHaveURL(/\/public-dashboard/);
  }
}
