import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';
import path from 'path';

// Load environment variables from .env file
dotenv.config({ path: path.resolve(__dirname, '.env') });

const baseURL = process.env.BASE_URL || 'http://localhost:8080';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [['html', { open: 'never' }], ['github']]
    : [['html', { open: 'on-failure' }]],

  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    // Setup project for authentication
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
      teardown: 'cleanup',
    },
    {
      name: 'cleanup',
      testMatch: /global\.teardown\.ts/,
    },

    // Public page tests - no auth required
    {
      name: 'public',
      testDir: './tests/public',
      use: { ...devices['Desktop Chrome'] },
    },

    // Auth flow tests - tests the login/logout/registration process itself
    // No dependencies - these tests perform their own authentication
    {
      name: 'auth',
      testDir: './tests/auth',
      testIgnore: ['**/auth.setup.ts', '**/logout.spec.ts'],
      use: { ...devices['Desktop Chrome'] },
    },

    // Logout test - requires authenticated state
    {
      name: 'auth-logout',
      testDir: './tests/auth',
      testMatch: '**/logout.spec.ts',
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
    },

    // Authenticated tests - require login
    {
      name: 'authenticated',
      testDir: './tests/authenticated',
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
    },

    // API tests
    {
      name: 'api',
      testDir: './tests/api',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Configure web server if needed for local development
  // webServer: {
  //   command: 'npm run start',
  //   url: 'http://localhost:8080',
  //   reuseExistingServer: !process.env.CI,
  // },
});
