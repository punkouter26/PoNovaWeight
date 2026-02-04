import { test, expect } from '@playwright/test';

/**
 * End-to-end tests for the Dev Login UI button on the login page.
 * Tests the actual UI interaction (clicking the button) rather than calling the API directly.
 * REQUIRES: The API must be running at localhost:5000 in Development mode.
 */
test.describe('Dev Login UI Tests', () => {

  test('Dev login button click navigates to home page', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Wait for Blazor WASM to initialize and render the page
    await page.waitForSelector('text=Sign in with Google', { timeout: 30000 });

    // Verify dev login section is visible (only in Development mode)
    const devLoginSection = page.locator('text=Development Mode');
    await expect(devLoginSection).toBeVisible({ timeout: 5000 });

    // Find and click the dev login button
    const devLoginButton = page.locator('button:has-text("Dev Login")');
    await expect(devLoginButton).toBeVisible();
    await devLoginButton.click();

    // Wait for login to complete and redirect to home page
    // The button shows "Signing in..." during the request
    await page.waitForURL('**/', { timeout: 15000 });

    // Verify we're NOT on the login page anymore
    expect(page.url()).not.toContain('/login');
  });

  test('Dev login with custom email works', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Wait for Blazor to initialize
    await page.waitForSelector('text=Development Mode', { timeout: 30000 });

    // Clear the default email and enter a custom one
    const emailInput = page.locator('input[type="email"]');
    await emailInput.clear();
    await emailInput.fill('custom-test@example.com');

    // Click dev login button
    const devLoginButton = page.locator('button:has-text("Dev Login")');
    await devLoginButton.click();

    // Wait for redirect to home
    await page.waitForURL('**/', { timeout: 15000 });

    // Verify we're NOT on the login page (indicates successful login)
    expect(page.url()).not.toContain('/login');
  });

  test('Dev login section visible on localhost', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login', {
      timeout: 30000,
      waitUntil: 'domcontentloaded',
    });

    // Wait for page to fully render
    await page.waitForSelector('text=Sign in with Google', { timeout: 30000 });

    // In Development mode on localhost, the dev login section should be visible
    const devLoginSection = page.locator('text=Development Mode');
    const isVisible = await devLoginSection.isVisible();
    
    // We're running on localhost, so it should be visible
    expect(isVisible).toBe(true);
  });
});
