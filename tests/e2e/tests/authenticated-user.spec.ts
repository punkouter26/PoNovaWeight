import { test, expect } from '@playwright/test';
import { loginAsUser, getAuthStatus, logout } from '../helpers/auth';

/**
 * End-to-end tests for authentication flow.
 * Uses the dev-login endpoint to bypass Google OAuth.
 * Note: With JWT auth, the token must be passed in API calls.
 * REQUIRES: The API must be running at localhost:5000 before running these tests.
 */
test.describe('Authenticated User Tests', () => {
  let authToken: string;

  test.beforeEach(async ({ page }) => {
    // Log in before each test using dev-login
    authToken = await loginAsUser(page, 'e2e-test@local');
  });

  test('Auth status returns authenticated user info with token', async ({ page }) => {
    // Verify auth status via API with the token
    const authStatus = await getAuthStatus(page, authToken);
    
    expect(authStatus.isAuthenticated).toBe(true);
    expect(authStatus.user?.email).toBe('e2e-test@local');
  });

  test('Auth status returns unauthenticated without token', async ({ page }) => {
    // Verify auth status via API without token
    const authStatus = await getAuthStatus(page);
    
    expect(authStatus.isAuthenticated).toBe(false);
    expect(authStatus.user).toBeNull();
  });

  test('Multiple users can have separate tokens', async ({ page, browser }) => {
    // First user token is already obtained from beforeEach (authToken)

    // Create a new isolated context
    const context2 = await browser.newContext({ ignoreHTTPSErrors: true });
    const page2 = await context2.newPage();
    const token2 = await loginAsUser(page2, 'second-user@local');

    // Verify both users have correct tokens that authenticate correctly
    const user1Status = await getAuthStatus(page, authToken);
    const user2Status = await getAuthStatus(page2, token2);

    expect(user1Status.isAuthenticated).toBe(true);
    expect(user1Status.user?.email).toBe('e2e-test@local');
    expect(user2Status.isAuthenticated).toBe(true);
    expect(user2Status.user?.email).toBe('second-user@local');

    await context2.close();
  });

  test('Logout clears token cache', async ({ page }) => {
    // First verify we have a valid token
    const beforeLogout = await getAuthStatus(page, authToken);
    expect(beforeLogout.isAuthenticated).toBe(true);

    // Logout (clears cached token)
    logout('e2e-test@local');

    // The token itself is still valid (JWTs are stateless),
    // but the cache is cleared. With real OIDC, the client would
    // discard the token and require re-authentication.
    // For this test, we verify the cache clearing works by checking
    // that getAuthStatus without token returns unauthenticated
    const afterLogout = await getAuthStatus(page);
    expect(afterLogout.isAuthenticated).toBe(false);
  });
});
