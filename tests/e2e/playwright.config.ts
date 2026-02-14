import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for PoNovaWeight E2E tests.
 * @see https://playwright.dev/docs/test-configuration
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

  /* Start the API and Client before running tests */
  webServer: process.env.CI 
    ? {
        // In CI, use the pre-built API directly
        command: 'dotnet src/PoNovaWeight.Api/bin/Release/net10.0/PoNovaWeight.Api.dll',
        url: 'http://localhost:5000/health',
        reuseExistingServer: false,
        timeout: 60000,
        stdout: 'pipe',
        stderr: 'pipe',
      }
    : {
        // Local development - run API directly
        command: 'dotnet run --project ../src/PoNovaWeight.Api/PoNovaWeight.Api.csproj',
        url: 'http://localhost:5000/health',
        reuseExistingServer: !process.env.CI,
        timeout: 120000,
        stdout: 'pipe',
        stderr: 'pipe',
        ignoreHTTPSErrors: true,
      },
});
