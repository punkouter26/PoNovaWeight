import { test, expect } from '@playwright/test';

const ASPIRE_DASHBOARD_URL = 'http://localhost:15225';

/**
 * End-to-end tests for .NET Aspire Dashboard.
 * SKIPPED: The Aspire dashboard port is dynamically assigned and the tests
 * cannot reliably connect to it. These tests would need to parse the Aspire
 * startup output to get the correct port.
 * REQUIRES: The Aspire AppHost must be running before running these tests.
 */
test.describe.skip('Aspire Dashboard Tests', () => {
  
  test.beforeEach(async ({ context }) => {
    // Ignore HTTPS errors for Aspire dashboard
    await context.setExtraHTTPHeaders({});
  });

  test('Aspire dashboard loads successfully', async ({ page }) => {
    // Navigate to Aspire dashboard
    const response = await page.goto(ASPIRE_DASHBOARD_URL, {
      timeout: 30000,
      waitUntil: 'networkidle',
    });

    // Assert - Dashboard responds successfully
    expect(response).not.toBeNull();
    expect(response!.ok()).toBeTruthy();

    // Wait for the page to fully load
    await page.waitForLoadState('domcontentloaded');

    // Verify page title contains Aspire
    const title = await page.title();
    expect(title.toLowerCase()).toContain('aspire');
  });

  test('Aspire dashboard shows resources', async ({ page }) => {
    // Navigate to Aspire dashboard resources page
    await page.goto(ASPIRE_DASHBOARD_URL, {
      timeout: 30000,
      waitUntil: 'networkidle',
    });

    // Wait for the dashboard to initialize
    await page.waitForLoadState('domcontentloaded');
    
    // Give Blazor time to render
    await page.waitForTimeout(2000);

    // Assert - Page content loads
    const content = await page.content();
    
    // Dashboard should show resource information
    expect(content.trim()).not.toBe('');
    
    // Look for common Aspire dashboard elements (resources table, project names)
    const hasResourcesContent = 
      content.toLowerCase().includes('api') ||
      content.toLowerCase().includes('storage') ||
      content.toLowerCase().includes('resources');
    
    expect(hasResourcesContent).toBeTruthy();
  });
});
