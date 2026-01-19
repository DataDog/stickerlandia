import { type Locator, type Page, expect } from '@playwright/test';

export class LandingPage {
  readonly page: Page;
  readonly startCollectingButton: Locator;
  readonly viewPublicDashboardLink: Locator;
  readonly heroTitle: Locator;
  readonly heroDescription: Locator;
  readonly featureCards: Locator;

  constructor(page: Page) {
    this.page = page;
    this.startCollectingButton = page.getByRole('button', { name: /start collecting/i });
    this.viewPublicDashboardLink = page.getByRole('link', { name: /view public dashboard/i });
    this.heroTitle = page.locator('h1');
    this.heroDescription = page.locator('.landing-cta p').first();
    this.featureCards = page.locator('.landing-card');
  }

  async goto() {
    await this.page.goto('/');
  }

  async clickStartCollecting() {
    await this.startCollectingButton.click();
  }

  async clickViewPublicDashboard() {
    await this.viewPublicDashboardLink.click();
  }

  async expectToBeVisible() {
    await expect(this.heroTitle).toBeVisible();
    await expect(this.startCollectingButton).toBeVisible();
  }

  async expectHeroContent() {
    await expect(this.heroTitle).toContainText('Collect. Share. Print.');
    await expect(this.heroDescription).toContainText('Stickerlandia');
  }

  async expectFeatureCardsVisible() {
    await expect(this.featureCards).toHaveCount(3);
  }
}
