import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const AUTH_STATE_PATH = path.join(__dirname, '..', '.auth', 'google-state.json');
const TOKEN_PATH = path.join(__dirname, '..', '.auth', 'google-token.json');

/**
 * E2E tests using a real Google account.
 * 
 * Prerequisites:
 *   1. Run `npm run auth:setup` to log in with Google and save browser state
 *   2. The API must be running locally (https://localhost:5001)
 *
 * These tests use the saved browser state from step 1, so no interactive
 * login is needed during test execution.
 */
test.describe('Google Authenticated User', () => {
  // Skip all tests if auth state hasn't been saved
  test.skip(() => !fs.existsSync(AUTH_STATE_PATH), 'Run "npm run auth:setup" first to save Google auth state');

  // Use the saved Google auth state for all tests in this describe block
  test.use({
    storageState: AUTH_STATE_PATH,
    baseURL: 'https://localhost:5001',
    ignoreHTTPSErrors: true,
  });

  test('App loads without redirecting to login', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle');
    // Allow Blazor WASM to fully load and check auth
    await page.waitForTimeout(5000);

    const url = page.url();
    expect(url).not.toContain('/login');
    expect(url).not.toContain('/authentication');
  });

  test('Dashboard page is accessible', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    // Should see the main app content, not the login page
    const content = await page.content();
    const hasAppContent = content.includes('Dashboard') ||
      content.includes('Daily Log') ||
      content.includes('Calendar') ||
      content.includes('PoNovaWeight');
    expect(hasAppContent).toBeTruthy();
  });

  test('User identity is displayed', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    // The sidebar should show the user's name
    const content = await page.content();
    // Google accounts will have some display name shown
    const hasUserInfo = content.includes('Sign out') || content.includes('sign out');
    expect(hasUserInfo).toBeTruthy();
  });

  test('Daily log page loads for today', async ({ page }) => {
    const today = new Date().toISOString().split('T')[0];
    await page.goto(`/day/${today}`, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    const url = page.url();
    expect(url).not.toContain('/login');

    const content = await page.content();
    const hasDailyLog = content.includes('Daily Log') ||
      content.includes('Food Categories') ||
      content.includes('Water');
    expect(hasDailyLog).toBeTruthy();
  });

  test('Calendar page loads', async ({ page }) => {
    await page.goto('/calendar', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    const url = page.url();
    expect(url).not.toContain('/login');
  });
});

test.describe('Google Auth API Tests', () => {
  // Skip if token file doesn't exist
  test.skip(() => !fs.existsSync(TOKEN_PATH), 'Run "npm run auth:setup" first to save Google token');

  let idToken: string;

  test.beforeAll(() => {
    const tokenData = JSON.parse(fs.readFileSync(TOKEN_PATH, 'utf-8'));
    idToken = tokenData.id_token;
  });

  test('Auth/me returns authenticated user with Google token', async ({ request }) => {
    const response = await request.get('https://localhost:5001/api/auth/me', {
      headers: { 'Authorization': `Bearer ${idToken}` },
      ignoreHTTPSErrors: true,
    });
    expect(response.ok()).toBeTruthy();
    const data = await response.json();
    expect(data.isAuthenticated).toBe(true);
    expect(data.user).not.toBeNull();
    expect(data.user.email).toContain('@');
    console.log(`  Authenticated as: ${data.user.email} (${data.user.displayName})`);
  });

  test('Protected daily-logs endpoint works with Google token', async ({ request }) => {
    const today = new Date().toISOString().split('T')[0];
    const response = await request.get(`https://localhost:5001/api/daily-logs/${today}`, {
      headers: { 'Authorization': `Bearer ${idToken}` },
      ignoreHTTPSErrors: true,
    });
    // Should be 200 (with data) or 404 (no entry for today) - but NOT 401
    expect([200, 404]).toContain(response.status());
  });

  test('Auth sync endpoint works with Google token', async ({ request }) => {
    const response = await request.post('https://localhost:5001/api/auth/sync', {
      headers: { 'Authorization': `Bearer ${idToken}` },
      ignoreHTTPSErrors: true,
    });
    expect(response.ok()).toBeTruthy();
    const data = await response.json();
    expect(data.email).toContain('@');
  });
});
