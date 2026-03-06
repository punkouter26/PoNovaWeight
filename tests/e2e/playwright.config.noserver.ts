import { defineConfig, devices } from '@playwright/test';

/**
 * Temporary Playwright configuration for PoNovaWeight E2E tests.
 * Assumes API is already running on localhost:5000
 */
export default defineConfig({
  testDir: './tests',
  
  /* Run tests in files in parallel */
  fullyParallel: true,
  
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,
  
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  
  /* Opt out of parallel tests on CI */
  workers: process.env.CI ? 1 : undefined,
  
  /* Reporter to use */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list']
  ],
  
  /* Shared settings for all the projects below */
  use: {
    /* Base URL to use in actions like `await page.goto('/')` */
    baseURL: 'http://localhost:5000',

    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',
    
    /* Capture screenshot on failure */
    screenshot: 'only-on-failure',

    /* Ignore HTTPS errors for self-signed certs in dev */
    ignoreHTTPSErrors: true,
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    // Mobile viewport testing per PoTest requirements
    {
      name: 'mobile',
      use: { ...devices['Pixel 5'] },
    },
  ],

  /* Timeout for each test */
  timeout: 60000,
  
  /* Timeout for each expect() assertion */
  expect: {
    timeout: 10000,
  },
});
