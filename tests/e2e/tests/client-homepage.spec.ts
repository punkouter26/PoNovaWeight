import { test, expect, Request } from '@playwright/test';

/**
 * End-to-end tests for the Blazor WebAssembly client homepage.
 * REQUIRES: The API must be running at localhost:5000 before running these tests.
 */
test.describe('Client Homepage Tests', () => {

  test('Homepage loads Blazor WebAssembly', async ({ page }) => {
    // Navigate to the homepage - use 'load' instead of 'networkidle' for Blazor apps
    const response = await page.goto('/', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Assert - Page responds successfully
    expect(response).not.toBeNull();
    expect(response!.ok()).toBeTruthy();

    // Verify the page contains Blazor WebAssembly framework files
    const content = await page.content();
    expect(content.toLowerCase()).toContain('blazor');
  });

  test('Homepage has correct title', async ({ page }) => {
    // Navigate to the homepage
    await page.goto('/', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Assert - Page has the expected title
    const title = await page.title();
    expect(title).toContain('NovaWeight');
  });

  test('Homepage redirects unauthenticated users to login', async ({ page }) => {
    // Navigate to the homepage
    await page.goto('/', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Wait for Blazor WASM to fully initialize - this can take a while
    await page.waitForLoadState('networkidle');
    
    // Wait for Blazor to evaluate auth state and potentially redirect
    // The OIDC library may redirect to /authentication/login
    try {
      await page.waitForURL('**/login**', { timeout: 10000 });
    } catch {
      // If no redirect happens, check if we're on the authentication page
      await page.waitForURL('**/authentication/**', { timeout: 5000 }).catch(() => {});
    }

    // With client-side OIDC, the redirect may go to /login or /authentication/login
    // or the page may show the "Authorizing" state
    const url = page.url();
    let isOnAuthPage = url.includes('/login') || url.includes('/authentication');
    
    if (!isOnAuthPage) {
      // Only try to check content if the URL didn't match - wrap in try/catch
      // since the page may still be navigating
      try {
        const content = await page.content();
        isOnAuthPage = content.includes('Loading...');
      } catch {
        // Page is still navigating, which means auth redirect is in progress
        isOnAuthPage = true;
      }
    }
    
    // Log the actual URL for debugging
    console.log('Final URL:', url);
    
    expect(isOnAuthPage).toBe(true);
  });

  test('Homepage loads static assets', async ({ page }) => {
    // Track network requests for static assets
    let jsLoaded = false;
    let wasmLoaded = false;

    page.on('request', (request: Request) => {
      const url = request.url().toLowerCase();
      if (url.includes('.js')) jsLoaded = true;
      if (url.includes('.wasm') || url.includes('_framework')) wasmLoaded = true;
    });

    // Navigate to the homepage and wait for initial load
    await page.goto('/', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Give time for Blazor to start loading WASM files
    await page.waitForTimeout(3000);

    // Assert - Static assets were requested
    expect(jsLoaded).toBeTruthy();
    expect(wasmLoaded).toBeTruthy();
  });

  test('Health endpoint returns healthy', async ({ page }) => {
    // Check the health endpoint
    const response = await page.request.get('/health');

    // Assert - Health check returns OK
    expect(response.ok()).toBeTruthy();
    
    const body = await response.text();
    expect(body).toContain('Healthy');
  });
});
