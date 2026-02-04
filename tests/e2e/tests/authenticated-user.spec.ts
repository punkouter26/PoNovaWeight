import { test, expect } from '@playwright/test';
import { loginAsUser, getAuthStatus, logout } from '../helpers/auth';

/**
 * End-to-end tests that require authentication.
 * Uses the dev-login endpoint to bypass Google OAuth.
 * REQUIRES: The API must be running at localhost:5000 before running these tests.
 */
test.describe('Authenticated User Tests', () => {

  test.beforeEach(async ({ page }) => {
    // Log in before each test using dev-login
    await loginAsUser(page, 'e2e-test@local');
  });

  test('Authenticated user can access dashboard', async ({ page }) => {
    // Navigate to homepage (should redirect to dashboard, not login)
    await page.goto('/', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Wait for Blazor to initialize
    await page.waitForLoadState('networkidle');

    // Should NOT be redirected to login since we're authenticated
    expect(page.url()).not.toContain('/login');
  });

  test('Auth status returns authenticated user info', async ({ page }) => {
    // Verify auth status via API
    const authStatus = await getAuthStatus(page);
    
    expect(authStatus.isAuthenticated).toBe(true);
    expect(authStatus.user?.email).toBe('e2e-test@local');
  });

  test('Logout clears session and redirects to login', async ({ page }) => {
    // First verify we're logged in
    const beforeLogout = await getAuthStatus(page);
    expect(beforeLogout.isAuthenticated).toBe(true);

    // Logout
    await logout(page);

    // Wait for redirect to login
    await page.waitForURL('**/login**', { timeout: 10000 });
    expect(page.url()).toContain('/login');

    // Verify auth status is now unauthenticated
    const afterLogout = await getAuthStatus(page);
    expect(afterLogout.isAuthenticated).toBe(false);
  });

  test('Multiple users can have separate sessions', async ({ page, browser }) => {
    // First user is already logged in from beforeEach

    // Create a new isolated context (separate cookies)
    const context2 = await browser.newContext({ ignoreHTTPSErrors: true });
    const page2 = await context2.newPage();
    await loginAsUser(page2, 'second-user@local');

    // Verify both users have correct sessions
    const user1Status = await getAuthStatus(page);
    const user2Status = await getAuthStatus(page2);

    expect(user1Status.user?.email).toBe('e2e-test@local');
    expect(user2Status.user?.email).toBe('second-user@local');

    await context2.close();
  });
});
