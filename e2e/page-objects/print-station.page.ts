import { type Locator, type Page, expect } from '@playwright/test';

export class PrintStationPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly eventName: Locator;
  readonly switchEventLink: Locator;
  readonly registerPrinterButton: Locator;
  readonly printerGrid: Locator;
  readonly printerCards: Locator;
  readonly emptyState: Locator;
  readonly loadingIndicator: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.getByRole('heading', { name: 'Print Station' });
    this.eventName = page.locator('p.text-gray-600');
    this.switchEventLink = page.getByRole('link', { name: 'Switch Event' });
    this.registerPrinterButton = page.getByRole('button', { name: 'Register Printer' });
    this.printerGrid = page.locator('.grid.grid-cols-1');
    this.printerCards = page.locator('.landing-card');
    this.emptyState = page.getByText('No printers registered for this event yet.');
    this.loadingIndicator = page.locator('.animate-spin');
    this.errorMessage = page.locator('.text-red-500');
  }

  async goto(eventName: string) {
    await this.page.goto(`/print-station/${encodeURIComponent(eventName)}`);
  }

  async gotoSelector() {
    await this.page.goto('/print-station');
  }

  async expectToBeVisible() {
    await expect(this.pageTitle).toBeVisible();
  }

  async waitForPrintersLoaded() {
    await this.loadingIndicator.waitFor({ state: 'hidden', timeout: 15000 });
  }

  async getPrinterCount(): Promise<number> {
    return await this.printerCards.count();
  }

  getPrinterCard(index: number) {
    return this.printerCards.nth(index);
  }

  getActiveJobBadge(printerCard: Locator) {
    return printerCard.getByText(/\d+ in queue/);
  }

  getStatusBadge(printerCard: Locator) {
    return printerCard.getByText(/Online|Offline/);
  }

  getLastJobText(printerCard: Locator) {
    return printerCard.getByText(/Last job:|No jobs processed yet/);
  }

  getPrintButton(printerCard: Locator) {
    return printerCard.getByRole('button', { name: /Print|Offline/ });
  }
}

export class PrintDialogComponent {
  readonly page: Page;
  readonly dialog: Locator;
  readonly title: Locator;
  readonly stickerGrid: Locator;
  readonly stickerItems: Locator;
  readonly confirmationText: Locator;
  readonly selectionText: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly successMessage: Locator;
  readonly emptyState: Locator;
  readonly loadingIndicator: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.dialog = page.getByRole('dialog');
    this.title = this.dialog.locator('#print-dialog-title');
    this.stickerGrid = this.dialog.locator('.grid.grid-cols-3');
    this.stickerItems = this.stickerGrid.locator('> div');
    this.confirmationText = this.dialog.getByText('Confirm you want to print this sticker:');
    this.selectionText = this.dialog.getByText('Select a sticker from your collection to print:');
    this.submitButton = this.dialog.getByRole('button', { name: /^Print$|Submitting/ });
    this.cancelButton = this.dialog.getByRole('button', { name: /Cancel|Close/ });
    this.successMessage = this.dialog.getByText('Print job submitted!');
    this.emptyState = this.dialog.getByText("You don't have any stickers to print yet.");
    this.loadingIndicator = this.dialog.locator('.animate-spin');
    this.errorMessage = this.dialog.locator('.bg-red-50');
  }

  async waitForOpen() {
    await expect(this.dialog).toBeVisible({ timeout: 5000 });
  }

  async waitForStickersLoaded() {
    await this.loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
  }

  async selectSticker(index: number) {
    await this.stickerItems.nth(index).click();
  }

  async submit() {
    await this.submitButton.click();
  }

  async close() {
    await this.cancelButton.click();
  }
}
