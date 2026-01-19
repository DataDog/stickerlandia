import { test as teardown } from '@playwright/test';
import fs from 'fs';
import path from 'path';

teardown('cleanup auth state', async () => {
  // Clean up auth state file after all tests
  const authFile = path.join(__dirname, '..', '.auth', 'user.json');

  if (fs.existsSync(authFile)) {
    // In CI, we might want to keep the file for debugging
    if (!process.env.CI) {
      console.log('Cleaning up auth state file');
      // Optionally remove the file
      // fs.unlinkSync(authFile);
    }
  }
});
