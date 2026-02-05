import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config for running tests against an already-running server.
 * Use: npx playwright test --config playwright.local.config.ts
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  timeout: 60000,
  expect: { timeout: 10000 },
  // No webServer — expects the API to already be running
});
