import { test, expect } from '@playwright/test';

/**
 * CI-safe E2E Authentication Tests
 * 
 * These tests validate authentication flows without requiring:
 * - Real Google OAuth credentials
 * - Interactive browser interactions
 * - External service dependencies
 * 
 * They use the dev-login API endpoint which is safe for CI/CD environments.
 */

test.describe('CI-Safe Authentication Tests', () => {
  const API_URL = process.env.API_URL || 'http://localhost:5000';
  const API_BASE = process.env.API_BASE || 'http://localhost:5000';

  test('Dev login endpoint generates valid JWT token', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const testEmail = `test-${Date.now()}@example.com`;

    // Act
    const response = await request.post(devLoginUrl, {
      data: { email: testEmail }
    });

    // Assert
    expect(response.status()).toBe(200);
    const data = await response.json();
    expect(data).toHaveProperty('token');
    expect(typeof data.token).toBe('string');
    expect(data.token.split('.').length).toBe(3); // JWT has 3 parts
  });

  test('JWT token from dev-login can authenticate API requests', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const testEmail = `test-${Date.now()}@example.com`;

    // Act - Get token
    const loginResponse = await request.post(devLoginUrl, {
      data: { email: testEmail }
    });
    const { token } = await loginResponse.json();

    // Act - Use token to authenticate
    const authResponse = await request.get(authMeUrl, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    // Assert
    expect(authResponse.status()).toBe(200);
    const auth = await authResponse.json();
    expect(auth).toHaveProperty('email');
    expect(auth.email).toBe(testEmail);
  });

  test('Missing JWT token returns 401 Unauthorized', async ({ request }) => {
    // Arrange
    const authMeUrl = `${API_BASE}/api/auth/me`;

    // Act
    const response = await request.get(authMeUrl);

    // Assert
    expect(response.status()).toBe(401);
  });

  test('Invalid JWT token returns 401 Unauthorized', async ({ request }) => {
    // Arrange
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const invalidToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.invalid';

    // Act
    const response = await request.get(authMeUrl, {
      headers: {
        'Authorization': `Bearer ${invalidToken}`
      }
    });

    // Assert
    expect(response.status()).toBe(401);
  });

  test('Multiple users can have separate authentication tokens', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const user1Email = `user1-${Date.now()}@example.com`;
    const user2Email = `user2-${Date.now()}@example.com`;

    // Act - Create tokens for both users
    const login1 = await request.post(devLoginUrl, {
      data: { email: user1Email }
    });
    const login2 = await request.post(devLoginUrl, {
      data: { email: user2Email }
    });

    const { token: token1 } = await login1.json();
    const { token: token2 } = await login2.json();

    // Act - Verify each token authenticates the correct user
    const auth1 = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token1}` }
    });
    const auth2 = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token2}` }
    });

    // Assert
    const user1 = await auth1.json();
    const user2 = await auth2.json();
    expect(user1.email).toBe(user1Email);
    expect(user2.email).toBe(user2Email);
    expect(user1.email).not.toBe(user2.email);
  });

  test('Token persists across multiple API requests', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const testEmail = `test-${Date.now()}@example.com`;

    // Act - Get token
    const loginResponse = await request.post(devLoginUrl, {
      data: { email: testEmail }
    });
    const { token } = await loginResponse.json();

    // Act - Make multiple requests with same token
    const response1 = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const response2 = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const response3 = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    // Assert - Token remains valid across requests
    expect(response1.status()).toBe(200);
    expect(response2.status()).toBe(200);
    expect(response3.status()).toBe(200);

    const auth1 = await response1.json();
    const auth2 = await response2.json();
    const auth3 = await response3.json();
    expect(auth1.email).toBe(testEmail);
    expect(auth2.email).toBe(testEmail);
    expect(auth3.email).toBe(testEmail);
  });

  test('Dev login endpoint is only available in development', async ({ request }) => {
    // Note: In production, this endpoint should be disabled.
    // This test documents the expected behavior.
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;

    // This test should pass in development (returns 200)
    // and either return 404/405 in production or require auth
    const response = await request.post(devLoginUrl, {
      data: { email: 'test@example.com' }
    });

    // In development, endpoint works
    if (process.env.ENVIRONMENT === 'production') {
      // In production, should be disabled
      expect([404, 405]).toContain(response.status());
    } else {
      // In development, should work
      expect(response.status()).toBe(200);
    }
  });

  test('Protected API endpoint rejects unauthenticated requests', async ({ request }) => {
    // Arrange
    const healthUrl = `${API_BASE}/health`;
    const dailyLogsUrl = `${API_BASE}/api/daily-logs/2026-03-04`;

    // Act - Health endpoint is public
    const healthResponse = await request.get(healthUrl);
    // Act - Daily logs endpoint requires auth
    const dailyLogsResponse = await request.get(dailyLogsUrl);

    // Assert
    expect(healthResponse.status()).toBe(200);
    expect(dailyLogsResponse.status()).toBe(401); // Requires authentication
  });

  test('Token header is case-insensitive for Authorization', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const testEmail = `test-${Date.now()}@example.com`;

    // Act - Get token
    const loginResponse = await request.post(devLoginUrl, {
      data: { email: testEmail }
    });
    const { token } = await loginResponse.json();

    // Act - Test various header casing
    const responses = await Promise.all([
      request.get(authMeUrl, { headers: { 'Authorization': `Bearer ${token}` } }),
      request.get(authMeUrl, { headers: { 'authorization': `Bearer ${token}` } }),
      request.get(authMeUrl, { headers: { 'AUTHORIZATION': `Bearer ${token}` } })
    ]);

    // Assert - All should succeed (HTTP headers are case-insensitive)
    responses.forEach(response => {
      expect(response.status()).toBe(200);
    });
  });

  test('Bearer token requires proper "Bearer" prefix', async ({ request }) => {
    // Arrange
    const devLoginUrl = `${API_BASE}/api/auth/dev-login`;
    const authMeUrl = `${API_BASE}/api/auth/me`;
    const testEmail = `test-${Date.now()}@example.com`;

    // Act - Get token
    const loginResponse = await request.post(devLoginUrl, {
      data: { email: testEmail }
    });
    const { token } = await loginResponse.json();

    // Act - Try different auth header formats
    const bearerResponse = await request.get(authMeUrl, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const tokenOnlyResponse = await request.get(authMeUrl, {
      headers: { 'Authorization': token }
    });
    const basicAuthResponse = await request.get(authMeUrl, {
      headers: { 'Authorization': `Basic ${token}` }
    });

    // Assert
    expect(bearerResponse.status()).toBe(200);
    expect(tokenOnlyResponse.status()).toBe(401); // Missing "Bearer" prefix
    expect(basicAuthResponse.status()).toBe(401); // Wrong auth scheme
  });
});
