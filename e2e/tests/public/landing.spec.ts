import { test, expect } from '@playwright/test';
import { LandingPage } from '../../page-objects/landing.page';

test.describe('Landing Page', () => {
  let landingPage: LandingPage;

  test.beforeEach(async ({ page }) => {
    landingPage = new LandingPage(page);
    await landingPage.goto();
  });

  test('displays hero content correctly', async () => {
    await landingPage.expectToBeVisible();
    await landingPage.expectHeroContent();
  });

  test('shows three feature cards', async () => {
    await landingPage.expectFeatureCardsVisible();
  });

  test('has working Start Collecting button', async ({ page }) => {
    await expect(landingPage.startCollectingButton).toBeVisible();
    await expect(landingPage.startCollectingButton).toBeEnabled();
  });

  test('has working View Public Dashboard link', async ({ page }) => {
    await landingPage.clickViewPublicDashboard();
    await expect(page).toHaveURL(/\/public-dashboard/);
  });

  test('is responsive on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await landingPage.goto();

    await expect(landingPage.heroTitle).toBeVisible();
    await expect(landingPage.startCollectingButton).toBeVisible();
  });

  test('feature cards display correct content', async ({ page }) => {
    const cards = landingPage.featureCards;

    // Check first card - Earn Through Achievements
    await expect(cards.nth(0)).toContainText('Earn Through Achievements');

    // Check second card - Share Your Collection
    await expect(cards.nth(1)).toContainText('Share Your Collection');

    // Check third card - Print at Events
    await expect(cards.nth(2)).toContainText('Print at Events');
  });
});
