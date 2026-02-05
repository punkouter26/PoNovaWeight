import { test, expect } from '@playwright/test';
import { devLogin, getAuthStatus } from '../helpers/auth';

/**
 * End-to-end tests for the Dev Login API endpoint.
 * With client-side OIDC, there's no dev-login UI - only an API endpoint for E2E testing.
 * REQUIRES: The API must be running at localhost:5000 in Development mode.
 */
test.describe('Dev Login API Tests', () => {

  test('Dev login API returns valid JWT token', async ({ request }) => {
    // Call the dev-login API endpoint
    const result = await devLogin(request, 'api-test@local');
    
    // Verify we got a valid response
    expect(result.email).toBe('api-test@local');
    expect(result.displayName).toBe('api-test');
    expect(result.token).toBeDefined();
    expect(result.token.length).toBeGreaterThan(50); // JWTs are long
  });

  test('Dev login API works with custom email', async ({ request }) => {
    // Call with a custom email
    const result = await devLogin(request, 'custom-user@example.com');
    
    expect(result.email).toBe('custom-user@example.com');
    expect(result.displayName).toBe('custom-user');
    expect(result.token).toBeDefined();
  });

  test('Token from dev-login works with auth/me endpoint', async ({ page }) => {
    // Get a token from dev-login
    const response = await page.request.post('/api/auth/dev-login?email=token-test@local');
    expect(response.ok()).toBeTruthy();
    
    const result = await response.json();
    expect(result.isAuthenticated).toBe(true);
    
    // Use the token to call /api/auth/me
    const meResponse = await page.request.get('/api/auth/me', {
      headers: {
        'Authorization': `Bearer ${result.token}`
      }
    });
    
    expect(meResponse.ok()).toBeTruthy();
    const authStatus = await meResponse.json();
    expect(authStatus.isAuthenticated).toBe(true);
    expect(authStatus.user.email).toBe('token-test@local');
  });
});
