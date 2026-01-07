import { test, expect } from '@playwright/test';
import { getTestCredentials } from '../helpers/keyvault';

/**
 * End-to-end tests for Google OAuth authentication flow.
 * These tests verify the application's authentication using real Google credentials from Azure Key Vault.
 * REQUIRES: 
 * - The application must be running at localhost:5000
 * - Azure CLI login with access to ponovaweight-kv Key Vault
 */
test.describe('Google Auth Tests', () => {
  
  test('Login page displays correct UI elements', async ({ page }) => {
    // Navigate to login page - use domcontentloaded instead of networkidle for Blazor
    await page.goto('/login', {
      waitUntil: 'domcontentloaded',
    });
    
    // Wait for Blazor WASM to initialize and render the sign-in button
    await page.waitForSelector('text=Sign in with Google', { timeout: 30000 });

    // Assert - Sign in button exists
    const signInButton = await page.getByText('Sign in with Google').count();
    expect(signInButton).toBe(1);

    // Assert - Title contains NovaWeight
    const title = await page.title();
    expect(title).toContain('NovaWeight');

    // Assert - SVG elements exist
    const svgCount = await page.locator('svg').count();
    expect(svgCount).toBeGreaterThanOrEqual(1);

    // Assert - Terms of service text exists
    const content = await page.content();
    expect(content.toLowerCase()).toContain('terms of service');
  });

  test('Auth flow works correctly', async ({ page }) => {
    // Test homepage redirect - navigate and wait for Blazor to redirect
    await page.goto('/', {
      waitUntil: 'domcontentloaded',
    });
    
    // Wait for Blazor to initialize and redirect to login
    await page.waitForURL('**/login**', { timeout: 30000 });
    expect(page.url()).toContain('/login');

    // Test auth/me returns unauthenticated
    const response = await page.request.get('/api/auth/me');
    expect(response.ok()).toBeTruthy();
    const body = await response.text();
    expect(body.toLowerCase()).toContain('false');

    // Test logout redirects
    await page.goto('/api/auth/logout', {
      waitUntil: 'domcontentloaded',
    });
    
    // Wait for redirect after logout
    await page.waitForURL('**/login**', { timeout: 30000 });
    expect(page.url()).toContain('/login');
  });

  test('Full Google OAuth login flow', async ({ page }) => {
    // Fetch credentials from Azure Key Vault
    const { email, password } = await getTestCredentials();

    // Navigate to login page
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    await page.waitForSelector('text=Sign in with Google', { timeout: 30000 });

    // Click sign in with Google button - this will navigate to Google
    await page.click('text=Sign in with Google');

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

    // Verify auth/me returns authenticated
    const response = await page.request.get('/api/auth/me');
    expect(response.ok()).toBeTruthy();
    const authStatus = await response.json();
    expect(authStatus.isAuthenticated).toBe(true);
    expect(authStatus.user.email).toBe(email);
  });
});
