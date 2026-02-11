import { test, expect } from '@playwright/test';
import { restoreSessionToken } from '../../fixtures/auth.fixture';
import { PrintStationPage } from '../../page-objects/print-station.page';

test.describe('Print Station - Admin Visibility', () => {
  test('admin sees Print Station in sidebar navigation', async ({ page }) => {
    // Restore the admin JWT from localStorage backup into sessionStorage
    await restoreSessionToken(page);

    // Navigate to catalogue (a page with sidebar) to check nav items
    await page.goto('/catalogue');
    await page.waitForLoadState('networkidle');

    const printStationLink = page.getByRole('link', { name: 'Print Station' });
    await expect(printStationLink).toBeVisible({ timeout: 10000 });
  });

  test('admin can navigate to print station via sidebar', async ({ page }) => {
    await restoreSessionToken(page);

    await page.goto('/catalogue');
    await page.waitForLoadState('networkidle');

    const printStationLink = page.getByRole('link', { name: 'Print Station' });
    await expect(printStationLink).toBeVisible({ timeout: 10000 });
    await printStationLink.click();

    await expect(page).toHaveURL(/\/print-station/);
  });
});

test.describe('Print Station - Printer Queue Display', () => {
  test('printer cards show status badge', async ({ page }) => {
    await restoreSessionToken(page);

    const printStation = new PrintStationPage(page);

    // Navigate to event selector first
    await printStation.gotoSelector();
    await page.waitForLoadState('networkidle');

    // Check if any events exist by looking for event links
    const eventLinks = page.locator('a[href^="/print-station/"]');
    const eventCount = await eventLinks.count();

    if (eventCount === 0) {
      // No events — navigate directly to a test event
      await printStation.goto('e2e-test-event');
      await page.waitForLoadState('networkidle');
      await printStation.waitForPrintersLoaded();

      // Should show empty state
      await expect(printStation.emptyState).toBeVisible();
      return;
    }

    // Click the first event
    await eventLinks.first().click();
    await page.waitForLoadState('networkidle');
    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();

    if (printerCount === 0) {
      await expect(printStation.emptyState).toBeVisible();
      return;
    }

    // Verify each printer card has the expected structure
    for (let i = 0; i < printerCount; i++) {
      const card = printStation.getPrinterCard(i);

      const statusBadge = printStation.getStatusBadge(card);
      await expect(statusBadge).toBeVisible();

      const lastJobText = printStation.getLastJobText(card);
      await expect(lastJobText).toBeVisible();

      const printButton = printStation.getPrintButton(card);
      await expect(printButton).toBeVisible();
    }
  });

  test('printer cards show active job count when jobs are queued', async ({ page }) => {
    await restoreSessionToken(page);

    const printStation = new PrintStationPage(page);

    // Intercept the status API to inject a known activeJobCount
    await page.route('**/printers/status', async (route) => {
      const response = await route.fetch();
      const body = await response.json();

      if (body?.data?.printers) {
        body.data.printers = body.data.printers.map((printer: Record<string, unknown>) => ({
          ...printer,
          activeJobCount: 3,
        }));
      }

      await route.fulfill({
        response,
        json: body,
      });
    });

    await printStation.gotoSelector();
    await page.waitForLoadState('networkidle');

    const eventLinks = page.locator('a[href^="/print-station/"]');
    const eventCount = await eventLinks.count();

    if (eventCount === 0) {
      test.skip();
      return;
    }

    await eventLinks.first().click();
    await page.waitForLoadState('networkidle');
    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();
    if (printerCount === 0) {
      test.skip();
      return;
    }

    // Verify each card shows the injected queue count
    for (let i = 0; i < printerCount; i++) {
      const card = printStation.getPrinterCard(i);
      const queueBadge = printStation.getActiveJobBadge(card);
      await expect(queueBadge).toBeVisible();
      await expect(queueBadge).toHaveText('3 in queue');
    }
  });

  test('printer cards hide queue badge when no active jobs', async ({ page }) => {
    await restoreSessionToken(page);

    const printStation = new PrintStationPage(page);

    // Intercept the status API to set activeJobCount to 0
    await page.route('**/printers/status', async (route) => {
      const response = await route.fetch();
      const body = await response.json();

      if (body?.data?.printers) {
        body.data.printers = body.data.printers.map((printer: Record<string, unknown>) => ({
          ...printer,
          activeJobCount: 0,
        }));
      }

      await route.fulfill({
        response,
        json: body,
      });
    });

    await printStation.gotoSelector();
    await page.waitForLoadState('networkidle');

    const eventLinks = page.locator('a[href^="/print-station/"]');
    const eventCount = await eventLinks.count();

    if (eventCount === 0) {
      test.skip();
      return;
    }

    await eventLinks.first().click();
    await page.waitForLoadState('networkidle');
    await printStation.waitForPrintersLoaded();

    const printerCount = await printStation.getPrinterCount();
    if (printerCount === 0) {
      test.skip();
      return;
    }

    // Queue badge should NOT be visible when count is 0
    for (let i = 0; i < printerCount; i++) {
      const card = printStation.getPrinterCard(i);
      const queueBadge = printStation.getActiveJobBadge(card);
      await expect(queueBadge).not.toBeVisible();
    }
  });
});
