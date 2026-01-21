import { test, expect } from '@playwright/test';

test.describe('API Authentication Requirements', () => {
  test.describe('Sticker Catalogue - Protected Endpoints', () => {
    test('POST /api/stickers/v1 requires authentication', async ({ request }) => {
      const response = await request.post('/api/stickers/v1', {
        data: {
          stickerName: 'Test Sticker',
          stickerDescription: 'A test sticker',
        },
      });
      expect(response.status()).toBe(401);
    });

    test('PUT /api/stickers/v1/{id} requires authentication', async ({ request }) => {
      const response = await request.put('/api/stickers/v1/sticker-001', {
        data: {
          stickerName: 'Updated Sticker',
        },
      });
      expect(response.status()).toBe(401);
    });

    test('DELETE /api/stickers/v1/{id} requires authentication', async ({ request }) => {
      const response = await request.delete('/api/stickers/v1/sticker-001');
      expect(response.status()).toBe(401);
    });

    test('POST /api/stickers/v1/{id}/image requires authentication', async ({ request }) => {
      const response = await request.post('/api/stickers/v1/sticker-001/image', {
        headers: {
          'Content-Type': 'image/png',
        },
        data: Buffer.from('fake-image-data'),
      });
      expect(response.status()).toBe(401);
    });
  });

  test.describe('Sticker Catalogue - Public Endpoints', () => {
    test('GET /api/stickers/v1 does not require authentication', async ({ request }) => {
      const response = await request.get('/api/stickers/v1');
      expect(response.ok()).toBeTruthy();
    });

    test('GET /api/stickers/v1/{id} does not require authentication', async ({ request }) => {
      const response = await request.get('/api/stickers/v1/sticker-001');
      expect(response.ok()).toBeTruthy();
    });

    test('GET /api/stickers/v1/{id}/image does not require authentication', async ({ request }) => {
      const response = await request.get('/api/stickers/v1/sticker-001/image');
      expect(response.ok()).toBeTruthy();
    });
  });
});
