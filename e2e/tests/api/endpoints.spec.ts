import { test, expect } from '@playwright/test';

test.describe('API Endpoints', () => {
  test.describe('Health Checks', () => {
    test('web-backend health endpoint returns 200', async ({ request }) => {
      const response = await request.get('/api/health');
      expect(response.ok()).toBeTruthy();
    });

    test('user-management health endpoint returns 200', async ({ request }) => {
      const response = await request.get('/api/users/v1/health');
      expect(response.ok()).toBeTruthy();
    });
  });

  test.describe('Sticker Catalogue API', () => {
    test('returns valid sticker list', async ({ request }) => {
      const response = await request.get('/api/stickers/v1/');
      expect(response.ok()).toBeTruthy();

      const data = await response.json();
      expect(data).toHaveProperty('stickers');
      expect(Array.isArray(data.stickers)).toBeTruthy();
      expect(data.stickers.length).toBeGreaterThan(0);

      const sticker = data.stickers[0];
      expect(sticker).toHaveProperty('stickerId');
      expect(sticker).toHaveProperty('stickerName');
    });

    test('supports pagination parameters', async ({ request }) => {
      const response = await request.get('/api/stickers/v1/?page=0&size=5');
      expect(response.ok()).toBeTruthy();

      const data = await response.json();
      expect(data).toHaveProperty('stickers');
      expect(data).toHaveProperty('pagination');
      expect(data.pagination).toHaveProperty('page');
      expect(data.pagination).toHaveProperty('size');
      expect(data.pagination).toHaveProperty('total');
      expect(data.pagination).toHaveProperty('totalPages');
    });

    test('returns individual sticker by ID', async ({ request }) => {
      // First get a valid ID
      const listResponse = await request.get('/api/stickers/v1/');
      const listData = await listResponse.json();
      const stickerId = listData.stickers[0]?.stickerId;

      if (stickerId) {
        const response = await request.get(`/api/stickers/v1/${stickerId}`);
        expect(response.ok()).toBeTruthy();

        const sticker = await response.json();
        expect(sticker).toHaveProperty('stickerId', stickerId);
        expect(sticker).toHaveProperty('stickerName');
        expect(sticker).toHaveProperty('stickerQuantityRemaining');
      }
    });

    test('returns 404 for non-existent sticker', async ({ request }) => {
      const response = await request.get('/api/stickers/v1/non-existent-id-xyz');
      expect(response.status()).toBe(404);
    });
  });
});
