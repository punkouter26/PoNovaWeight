import { Page, APIRequestContext, expect } from '@playwright/test';

const BASE_URL = 'http://localhost:5000';

/**
 * Authentication helper for E2E tests.
 * Uses the dev-login endpoint to bypass Google OAuth.
 */

/**
 * Logs in using the dev-login endpoint (Development environment only).
 * This creates a real cookie session without Google OAuth.
 * 
 * @param request - Playwright API request context
 * @param email - Email to use for the test user (default: dev-user@local)
 * @returns The authentication response with session cookie
 */
export async function devLogin(
  request: APIRequestContext,
  email: string = 'dev-user@local'
): Promise<{ email: string; displayName: string }> {
  const response = await request.post(`${BASE_URL}/api/auth/dev-login?email=${encodeURIComponent(email)}`, {
    ignoreHTTPSErrors: true,
  });
  
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Dev login failed: ${response.status()} - ${body}`);
  }
  
  const authStatus = await response.json();
  expect(authStatus.isAuthenticated).toBe(true);
  expect(authStatus.user.email).toBe(email);
  
  return {
    email: authStatus.user.email,
    displayName: authStatus.user.displayName
  };
}

/**
 * Logs in via dev-login and applies the session cookie to the page context.
 * Use this when you need a page that is already authenticated.
 * 
 * @param page - Playwright page
 * @param email - Email to use for the test user
 */
export async function loginAsUser(page: Page, email: string = 'dev-user@local'): Promise<void> {
  // Use the page's request context to ensure cookies are shared
  const response = await page.request.post(`/api/auth/dev-login?email=${encodeURIComponent(email)}`);
  
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Dev login failed: ${response.status()} - ${body}`);
  }
  
  const authStatus = await response.json();
  expect(authStatus.isAuthenticated).toBe(true);
}

/**
 * Verifies the current authentication status via the /api/auth/me endpoint.
 * 
 * @param page - Playwright page
 * @returns The authentication status
 */
export async function getAuthStatus(page: Page): Promise<{ isAuthenticated: boolean; user?: { email: string; displayName: string } }> {
  const response = await page.request.get('/api/auth/me');
  expect(response.ok()).toBeTruthy();
  return await response.json();
}

/**
 * Logs out the current user.
 * 
 * @param page - Playwright page
 */
export async function logout(page: Page): Promise<void> {
  await page.goto('/api/auth/logout', { waitUntil: 'domcontentloaded' });
}
