import { test, expect } from '@playwright/test';
import { CollectionPage, StickerDetailPage } from '../../page-objects/collection.page';
import { CataloguePage } from '../../page-objects/catalogue.page';
import { PrintStationPage, PrintDialogComponent } from '../../page-objects/print-station.page';
import { restoreSessionToken } from '../../fixtures/auth.fixture';

test.describe('Print Sticker - Owned Stickers', () => {
  test.use({ storageState: '.auth/user.json' });

  test.beforeEach(async ({ page }) => {
    await restoreSessionToken(page);
  });

  test('owned sticker in collection shows print button', async ({ page }) => {
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const stickerCount = await collection.stickerCards.count();
    if (stickerCount === 0) {
      test.skip(true, 'User has no stickers in their collection');
      return;
    }

    // Click the first owned sticker
    await collection.clickSticker(0);
    await expect(page).toHaveURL(/\/stickers\/[\w-]+/);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();

    // Owned sticker should show the print button
    await expect(detail.printButton).toBeVisible();
  });

  test('print button navigates to print station', async ({ page }) => {
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const stickerCount = await collection.stickerCards.count();
    if (stickerCount === 0) {
      test.skip(true, 'User has no stickers in their collection');
      return;
    }

    await collection.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await expect(detail.printButton).toBeVisible();

    await detail.printButton.click();

    // Should navigate to print station (with or without event name)
    await expect(page).toHaveURL(/\/print-station/);
  });

  test('print button navigates to print station with event selector or event page', async ({ page }) => {
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const stickerCount = await collection.stickerCards.count();
    if (stickerCount === 0) {
      test.skip(true, 'User has no stickers in their collection');
      return;
    }

    await collection.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await detail.printButton.click();

    await expect(page).toHaveURL(/\/print-station/);

    // Should land on either the event selector or a specific event's print station
    const isOnEventSelector = await page.getByText('Select an Event').isVisible().catch(() => false);
    const isOnPrintStation = await page.getByRole('heading', { name: 'Print Station' }).isVisible().catch(() => false);

    expect(isOnEventSelector || isOnPrintStation).toBeTruthy();
  });
});

test.describe('Print Sticker - Unowned Stickers', () => {
  test.use({ storageState: '.auth/user.json' });

  test.beforeEach(async ({ page }) => {
    await restoreSessionToken(page);
  });

  test('unowned sticker from catalogue does NOT show print button', async ({ page }) => {
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();

    const totalStickers = await catalogue.getStickerCount();
    if (totalStickers === 0) {
      test.skip(true, 'No stickers in catalogue');
      return;
    }

    // Check multiple stickers from catalogue until we find one the user does NOT own
    let foundUnownedSticker = false;

    for (let i = 0; i < Math.min(totalStickers, 5); i++) {
      await catalogue.goto();
      await catalogue.waitForStickersLoaded();
      await catalogue.clickSticker(i);

      const detail = new StickerDetailPage(page);
      await detail.waitForLoaded();

      // Wait for the ownership check API call to complete
      await page.waitForLoadState('networkidle');

      const isVisible = await detail.printButton.isVisible().catch(() => false);

      if (!isVisible) {
        // Found an unowned sticker — print button is correctly hidden
        foundUnownedSticker = true;
        await expect(detail.printButton).not.toBeVisible();
        break;
      }
    }

    if (!foundUnownedSticker) {
      test.skip(true, 'User owns all stickers in catalogue; cannot verify unowned behavior');
    }
  });

  test('owned sticker viewed from catalogue DOES show print button', async ({ page }) => {
    // Get user's collection first to know an owned sticker ID
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const ownedCount = await collection.stickerCards.count();
    if (ownedCount === 0) {
      test.skip(true, 'User has no stickers in their collection');
      return;
    }

    // Click the first owned sticker to get its ID from the URL
    await collection.clickSticker(0);
    await expect(page).toHaveURL(/\/stickers\/[\w-]+/);

    const url = page.url();
    const stickerId = url.split('/stickers/')[1];

    // Navigate to the catalogue, find this sticker, and click it (SPA navigation)
    const catalogue = new CataloguePage(page);
    await catalogue.goto();
    await catalogue.waitForStickersLoaded();

    // Click the sticker card that links to this sticker ID
    const stickerLink = page.locator(`a[href="/stickers/${stickerId}"]`);
    const linkExists = await stickerLink.count() > 0;

    if (!linkExists) {
      test.skip(true, 'Owned sticker not found on first page of catalogue');
      return;
    }

    await stickerLink.click();

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await page.waitForLoadState('networkidle');

    // This sticker is owned, so the print button should be visible
    await expect(detail.printButton).toBeVisible();
  });
});

test.describe('Print Sticker - Print Dialog Flow', () => {
  test.use({ storageState: '.auth/user.json' });

  test.beforeEach(async ({ page }) => {
    await restoreSessionToken(page);
  });

  test('print dialog shows pre-selected sticker with confirmation text', async ({ page }) => {
    // Navigate from collection → sticker detail → print station → click a printer
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const stickerCount = await collection.stickerCards.count();
    if (stickerCount === 0) {
      test.skip(true, 'User has no stickers in their collection');
      return;
    }

    await collection.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await detail.printButton.click();

    await expect(page).toHaveURL(/\/print-station/);

    const printStation = new PrintStationPage(page);

    // Wait for the page to settle — might be on event selector or print station
    await page.waitForLoadState('networkidle');

    // If we landed on the event selector (no last active event), we need an event
    const isOnEventSelector = await page.getByText('Select an Event').isVisible().catch(() => false);
    if (isOnEventSelector) {
      const eventLinks = page.locator('a[href^="/print-station/"]');
      const eventCount = await eventLinks.count();
      if (eventCount === 0) {
        test.skip(true, 'No events available for printing');
        return;
      }
      await eventLinks.first().click();
      await page.waitForLoadState('networkidle');
    }

    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();
    if (printerCount === 0) {
      test.skip(true, 'No printers registered for this event');
      return;
    }

    // Click print on the first online printer
    const firstCard = printStation.getPrinterCard(0);
    const printBtn = printStation.getPrintButton(firstCard);

    const isDisabled = await printBtn.isDisabled();
    if (isDisabled) {
      test.skip(true, 'No online printers available');
      return;
    }

    await printBtn.click();

    // Print dialog should open with pre-selected sticker
    const printDialog = new PrintDialogComponent(page);
    await printDialog.waitForOpen();

    // Should show confirmation text (not selection text) since sticker was pre-selected
    await expect(printDialog.confirmationText).toBeVisible();

    // The submit button should be enabled (sticker is already selected)
    await expect(printDialog.submitButton).toBeEnabled();

    // Close the dialog without submitting
    await printDialog.close();
    await expect(printDialog.dialog).not.toBeVisible();
  });

  test('print dialog without pre-selection shows user sticker collection', async ({ page }) => {
    // Navigate directly to print station (without pre-selected sticker)
    const printStation = new PrintStationPage(page);
    await printStation.gotoSelector();
    await page.waitForLoadState('networkidle');

    // Find an event with printers
    const eventLinks = page.locator('a[href^="/print-station/"]');
    const eventCount = await eventLinks.count();
    if (eventCount === 0) {
      test.skip(true, 'No events available');
      return;
    }

    await eventLinks.first().click();
    await page.waitForLoadState('networkidle');
    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();
    if (printerCount === 0) {
      test.skip(true, 'No printers registered');
      return;
    }

    // Click print on the first available printer
    const firstCard = printStation.getPrinterCard(0);
    const printBtn = printStation.getPrintButton(firstCard);

    const isDisabled = await printBtn.isDisabled();
    if (isDisabled) {
      test.skip(true, 'No online printers available');
      return;
    }

    await printBtn.click();

    const printDialog = new PrintDialogComponent(page);
    await printDialog.waitForOpen();
    await printDialog.waitForStickersLoaded();

    // Without pre-selection, should show "Select a sticker" text
    await expect(printDialog.selectionText).toBeVisible();

    // Should show user's sticker collection or empty state
    const hasStickers = await printDialog.stickerItems.count() > 0;
    const hasEmptyState = await printDialog.emptyState.isVisible().catch(() => false);

    expect(hasStickers || hasEmptyState).toBeTruthy();

    await printDialog.close();
  });

  test('can submit print job for owned sticker', async ({ page }) => {
    const collection = new CollectionPage(page);
    await collection.goto();
    await collection.waitForLoaded();

    const stickerCount = await collection.stickerCards.count();
    if (stickerCount === 0) {
      test.skip(true, 'User has no stickers');
      return;
    }

    await collection.clickSticker(0);

    const detail = new StickerDetailPage(page);
    await detail.waitForLoaded();
    await detail.printButton.click();

    await expect(page).toHaveURL(/\/print-station/);
    await page.waitForLoadState('networkidle');

    // Handle event selector if needed
    const isOnEventSelector = await page.getByText('Select an Event').isVisible().catch(() => false);
    if (isOnEventSelector) {
      const eventLinks = page.locator('a[href^="/print-station/"]');
      const eventCount = await eventLinks.count();
      if (eventCount === 0) {
        test.skip(true, 'No events available');
        return;
      }
      await eventLinks.first().click();
      await page.waitForLoadState('networkidle');
    }

    const printStation = new PrintStationPage(page);
    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();
    if (printerCount === 0) {
      test.skip(true, 'No printers registered');
      return;
    }

    const firstCard = printStation.getPrinterCard(0);
    const printBtn = printStation.getPrintButton(firstCard);

    if (await printBtn.isDisabled()) {
      test.skip(true, 'No online printers');
      return;
    }

    // Intercept the print job API to avoid actually printing
    let capturedRequest: { url: string; body: Record<string, unknown> } | null = null;
    await page.route('**/api/print/v1/event/*/printer/*/jobs', async (route) => {
      capturedRequest = {
        url: route.request().url(),
        body: route.request().postDataJSON(),
      };
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ printJobId: 'test-job-id' }),
      });
    });

    await printBtn.click();

    const printDialog = new PrintDialogComponent(page);
    await printDialog.waitForOpen();
    await expect(printDialog.confirmationText).toBeVisible();

    // Submit the print job
    await printDialog.submit();

    // Should show success message
    await expect(printDialog.successMessage).toBeVisible({ timeout: 10000 });

    // Verify the API was called with expected fields
    expect(capturedRequest).not.toBeNull();
    expect(capturedRequest!.body).toHaveProperty('stickerId');
    expect(capturedRequest!.body).toHaveProperty('stickerUrl');
    expect(capturedRequest!.body).toHaveProperty('userId');

    // Close the success dialog
    await printDialog.close();
  });
});
