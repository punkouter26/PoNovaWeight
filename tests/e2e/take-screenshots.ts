import { chromium } from 'playwright';
import * as fs from 'fs';
import * as path from 'path';

const BASE_URL = 'http://localhost:5000';
const SCREENSHOTS_DIR = '../../docs/screenshots';

// Ensure screenshots directory exists
if (!fs.existsSync(SCREENSHOTS_DIR)) {
  fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
}

async function takeScreenshots() {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // Screenshot 1: Login Page
    console.log('📸 Capturing Login page...');
    await page.goto(`${BASE_URL}`, { waitUntil: 'networkidle' });
    await page.screenshot({ 
      path: path.join(SCREENSHOTS_DIR, '01_Login.png'),
      fullPage: true
    });
    console.log('✅ Login page saved');

    // Screenshot 2: Dev Test User Login
    console.log('📸 Logging in with dev credentials...');
    const devLoginBtn = page.locator('button:has-text("Dev Login"), a:has-text("Dev-Test")');
    if (await devLoginBtn.isVisible()) {
      await devLoginBtn.click();
      await page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {});
    } else {
      // Try dev login endpoint directly
      const response = await page.request.post(`${BASE_URL}/api/auth/dev-test-user-login`);
      if (response.ok()) {
        const data = await response.json();
        if (data.token) {
          // Store token in localStorage
          await page.evaluate((token: string) => {
            localStorage.setItem('jwtToken', token);
          }, data.token);
          await page.reload({ waitUntil: 'networkidle' });
        }
      }
    }

    // Screenshot 3: Dashboard
    console.log('📸 Capturing Dashboard page...');
    await page.goto(`${BASE_URL}/dashboard`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000); // Wait for charts to render
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '02_Dashboard.png'),
      fullPage: true
    });
    console.log('✅ Dashboard page saved');

    // Screenshot 4: Calendar View
    console.log('📸 Capturing Calendar page...');
    await page.goto(`${BASE_URL}/calendar`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '03_Calendar.png'),
      fullPage: true
    });
    console.log('✅ Calendar page saved');

    // Screenshot 5: Day Detail
    console.log('📸 Capturing Day Detail page...');
    const today = new Date().toISOString().split('T')[0];
    await page.goto(`${BASE_URL}/day/${today}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '04_DayDetail.png'),
      fullPage: true
    });
    console.log('✅ Day Detail page saved');

    // Screenshot 6: Meal Scan
    console.log('📸 Capturing Meal Scan page...');
    await page.goto(`${BASE_URL}/meal-scan`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '05_MealScan.png'),
      fullPage: true
    });
    console.log('✅ Meal Scan page saved');

    // Screenshot 7: Weekly Summary
    console.log('📸 Capturing Weekly Summary page...');
    await page.goto(`${BASE_URL}/weekly-summary`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '06_WeeklySummary.png'),
      fullPage: true
    });
    console.log('✅ Weekly Summary page saved');

    // Screenshot 8: Settings
    console.log('📸 Capturing Settings page...');
    await page.goto(`${BASE_URL}/settings`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, '07_Settings.png'),
      fullPage: true
    });
    console.log('✅ Settings page saved');

    console.log('\n✨ All screenshots captured successfully!');

  } catch (error) {
    const err = error instanceof Error ? error : new Error(String(error));
    console.error('❌ Error taking screenshots:', err.message);
  } finally {
    await browser.close();
  }
}

takeScreenshots();
