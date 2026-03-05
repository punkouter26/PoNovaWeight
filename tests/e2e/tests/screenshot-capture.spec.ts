import { test, expect } from '@playwright/test';

test('Capture UI Screenshots', async ({ page }, testInfo) => {
  // Login first using dev endpoint
  const baseUrl = 'http://localhost:5000';
  
  // Get JWT token from dev endpoint
  const response = await page.request.post('http://localhost:5000/api/auth/dev-test-user-login');
  const token = await response.text();
  
  // Set token in localStorage
  await page.context().addInitScript((token) => {
    localStorage.setItem('authToken', token);
  }, token);

  // Screenshot 1: Dashboard (main page)
  await page.goto(`${baseUrl}/`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: './screenshots/01-dashboard.png', fullPage: true });
  console.log('Captured Dashboard');

  // Screenshot 2: Calendar page
  await page.goto(`${baseUrl}/calendar`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: './screenshots/02-calendar.png', fullPage: true });
  console.log('Captured Calendar');

  // Screenshot 3: A day detail (if possible)
  // Try to click on a date in calendar
  const firstDate = page.locator('button:has-text("1")').first();
  if (await firstDate.isVisible()) {
    await firstDate.click();
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: './screenshots/03-day-detail.png', fullPage: true });
    console.log('Captured Day Detail');
  }

  // Screenshot 4: Mobile view - Dashboard
  await page.setViewportSize({ width: 375, height: 667 });
  await page.goto(`${baseUrl}/`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: './screenshots/04-dashboard-mobile.png', fullPage: true });
  console.log('Captured Dashboard Mobile');

  // Screenshot 5: Mobile view - Menu
  await page.reload();
  await page.waitForLoadState('networkidle');
  const menuButton = page.locator('button').filter({ hasText: /Menu|☰|toggle/i }).first();
  if (await menuButton.isVisible()) {
    await menuButton.click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: './screenshots/05-mobile-menu.png', fullPage: true });
    console.log('Captured Mobile Menu');
  }
});
