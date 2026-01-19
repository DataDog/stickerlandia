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
      expect(sticker).toHaveProperty('imagePath');
    });
  });
});
