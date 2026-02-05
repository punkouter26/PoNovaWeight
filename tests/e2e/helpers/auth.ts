import { Page, APIRequestContext, expect } from '@playwright/test';

const BASE_URL = 'http://localhost:5000';

/**
 * Authentication helper for E2E tests.
 * Uses the dev-login endpoint to bypass Google OAuth.
 * The dev-login endpoint returns a JWT token that must be sent in Authorization header.
 */

interface DevLoginResponse {
  token: string;
  isAuthenticated: boolean;
  user: {
    email: string;
    displayName: string;
  };
}

// Store tokens per email for reuse
const tokenCache = new Map<string, string>();

/**
 * Logs in using the dev-login endpoint (Development environment only).
 * This creates a fake JWT token for testing without Google OAuth.
 * 
 * @param request - Playwright API request context
 * @param email - Email to use for the test user (default: dev-user@local)
 * @returns The authentication response with JWT token
 */
export async function devLogin(
  request: APIRequestContext,
  email: string = 'dev-user@local'
): Promise<{ email: string; displayName: string; token: string }> {
  const response = await request.post(`${BASE_URL}/api/auth/dev-login?email=${encodeURIComponent(email)}`, {
    ignoreHTTPSErrors: true,
  });
  
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Dev login failed: ${response.status()} - ${body}`);
  }
  
  const result: DevLoginResponse = await response.json();
  expect(result.isAuthenticated).toBe(true);
  expect(result.user.email).toBe(email);
  
  // Cache the token
  tokenCache.set(email, result.token);
  
  return {
    email: result.user.email,
    displayName: result.user.displayName,
    token: result.token
  };
}

/**
 * Logs in via dev-login and stores the token for subsequent API calls.
 * For JWT auth, we can't use cookies - the token must be sent in headers.
 * 
 * @param page - Playwright page
 * @param email - Email to use for the test user
 * @returns The JWT token for use in Authorization headers
 */
export async function loginAsUser(page: Page, email: string = 'dev-user@local'): Promise<string> {
  const response = await page.request.post(`/api/auth/dev-login?email=${encodeURIComponent(email)}`);
  
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Dev login failed: ${response.status()} - ${body}`);
  }
  
  const result: DevLoginResponse = await response.json();
  expect(result.isAuthenticated).toBe(true);
  
  // Cache the token
  tokenCache.set(email, result.token);
  
  return result.token;
}

/**
 * Gets a cached token for a user, or logs in to get one.
 */
export async function getToken(page: Page, email: string = 'dev-user@local'): Promise<string> {
  const cached = tokenCache.get(email);
  if (cached) return cached;
  return await loginAsUser(page, email);
}

/**
 * Makes an authenticated API request using the JWT token.
 */
export async function authenticatedRequest(
  page: Page,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  url: string,
  options: { email?: string; body?: unknown } = {}
): Promise<Response> {
  const token = await getToken(page, options.email);
  
  const fetchOptions: RequestInit = {
    method,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };
  
  if (options.body) {
    fetchOptions.body = JSON.stringify(options.body);
  }
  
  // Use page.evaluate to make the fetch from the browser context
  return await page.evaluate(async ({ url, options }) => {
    const response = await fetch(url, options);
    return {
      ok: response.ok,
      status: response.status,
      json: await response.json().catch(() => null),
    };
  }, { url, options: fetchOptions }) as unknown as Response;
}

/**
 * Verifies the current authentication status via the /api/auth/me endpoint.
 * Note: With JWT auth, this requires the token in Authorization header.
 * 
 * @param page - Playwright page
 * @param token - Optional JWT token (if not provided, returns unauthenticated)
 * @returns The authentication status
 */
export async function getAuthStatus(
  page: Page, 
  token?: string
): Promise<{ isAuthenticated: boolean; user?: { email: string; displayName: string } }> {
  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const response = await page.request.get('/api/auth/me', { headers });
  expect(response.ok()).toBeTruthy();
  return await response.json();
}

/**
 * Clears the token cache (simulates logout).
 * With JWT auth, logout is client-side only - just discard the token.
 * 
 * @param email - Optional email to clear specific token, or clears all if not provided
 */
export function logout(email?: string): void {
  if (email) {
    tokenCache.delete(email);
  } else {
    tokenCache.clear();
  }
}
