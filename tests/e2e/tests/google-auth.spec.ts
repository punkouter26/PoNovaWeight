import { test, expect } from '@playwright/test';
import { getTestCredentials } from '../helpers/keyvault';

/**
 * End-to-end tests for Google OAuth authentication flow.
 * With client-side OIDC, the Blazor client handles the OAuth flow directly with Google.
 * REQUIRES: 
 * - The application must be running at localhost:5000
 * - Azure CLI login with access to ponovaweight-kv Key Vault (for full OAuth test)
 */
test.describe('Google Auth Tests', () => {
  
  test('Login page displays correct UI elements', async ({ page }) => {
    // Navigate to login page - use domcontentloaded instead of networkidle for Blazor
    await page.goto('/login', {
      waitUntil: 'domcontentloaded',
      timeout: 30000,
    });
    
    // Wait for Blazor WASM to initialize - look for the login container
    await page.waitForLoadState('networkidle');
    
    // Give Blazor time to render
    await page.waitForTimeout(2000);

    // Assert - Page has loaded (check for key elements)
    const content = await page.content();
    expect(content.toLowerCase()).toContain('ponovaweight');

    // Assert - Title contains NovaWeight
    const title = await page.title();
    expect(title).toContain('NovaWeight');
  });

  test('Auth/me returns unauthenticated when no token', async ({ page }) => {
    // Test auth/me returns unauthenticated without a token
    const response = await page.request.get('/api/auth/me');
    expect(response.ok()).toBeTruthy();
    
    const authStatus = await response.json();
    expect(authStatus.isAuthenticated).toBe(false);
    expect(authStatus.user).toBeNull();
  });

  test('Protected API returns 401 without token', async ({ page }) => {
    // Try to access a protected endpoint without authentication
    const response = await page.request.get('/api/daily-logs/2025-01-01');
    
    // Should return 401 Unauthorized
    expect(response.status()).toBe(401);
  });

  test.skip('Full Google OAuth login flow', async ({ page }) => {
    // Skip this test by default - it requires real Google credentials
    // and interacts with external Google services
    
    // Fetch credentials from Azure Key Vault
    const { email, password } = await getTestCredentials();

    // Navigate to login page
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Click sign in with Google button - this will navigate to Google
    await page.click('button:has-text("Sign in with Google")');

    // Wait for Google's login page
    await page.waitForURL('**/accounts.google.com/**', { timeout: 30000 });

    // Fill in email
    await page.fill('input[type="email"]', email);
    await page.click('button:has-text("Next"), #identifierNext');

    // Wait for password field and fill it
    await page.waitForSelector('input[type="password"]', { timeout: 10000 });
    await page.fill('input[type="password"]', password);
    await page.click('button:has-text("Next"), #passwordNext');

    // Wait for redirect back to app after successful login
    await page.waitForURL('**/localhost:5000/**', { timeout: 30000 });

    // Verify we're logged in - should see dashboard or home page, not login
    expect(page.url()).not.toContain('/login');
  });
});
