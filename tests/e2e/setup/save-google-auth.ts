import { chromium } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Interactive setup script that opens a browser for you to log in with your real Google account.
 * After successful login, it saves the browser state (cookies, localStorage, sessionStorage)
 * so E2E tests can reuse it without needing to log in again.
 *
 * Usage:
 *   npx playwright test --config playwright.config.ts setup/save-google-auth.ts
 *   -- or --
 *   npx tsx setup/save-google-auth.ts
 *
 * The saved state expires when Google's session expires (typically a few weeks).
 * Re-run this script to refresh.
 */

const AUTH_STATE_PATH = path.join(__dirname, '..', '.auth', 'google-state.json');
const BASE_URL = process.env.BASE_URL ?? 'https://localhost:5001';

async function main() {
  // Ensure .auth directory exists
  const authDir = path.dirname(AUTH_STATE_PATH);
  if (!fs.existsSync(authDir)) {
    fs.mkdirSync(authDir, { recursive: true });
  }

  console.log('🔐 Google Auth Setup');
  console.log('====================');
  console.log('A browser window will open. Please:');
  console.log('  1. Click "Sign in with Google"');
  console.log('  2. Complete the Google login flow');
  console.log('  3. Wait until you see the app dashboard');
  console.log('  4. The browser will close automatically\n');

  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
  });
  const page = await context.newPage();

  // Navigate to login page
  await page.goto(`${BASE_URL}/login`, { waitUntil: 'networkidle', timeout: 30000 });
  console.log('📱 Browser opened at login page. Waiting for you to log in...\n');

  // Wait for the user to complete login and land back on the app (localhost)
  // Must be on localhost AND not on /login or /authentication pages
  try {
    await page.waitForURL(
      (url) => {
        const isLocalhost = url.hostname === 'localhost';
        const path = url.pathname;
        return isLocalhost && !path.includes('/login') && !path.includes('/authentication');
      },
      { timeout: 120000 } // 2 minutes to complete login
    );

    // Give the app a moment to fully initialize after login
    await page.waitForTimeout(3000);

    console.log(`✅ Login successful! Current URL: ${page.url()}`);

    // Save the storage state (cookies + localStorage)
    await context.storageState({ path: AUTH_STATE_PATH });
    console.log(`💾 Auth state saved to: ${AUTH_STATE_PATH}`);

    // Also save the id_token separately for API calls
    const idToken = await page.evaluate(() => {
      // Check sessionStorage for OIDC tokens
      for (let i = 0; i < sessionStorage.length; i++) {
        const key = sessionStorage.key(i);
        if (key && key.startsWith('oidc.')) {
          try {
            const val = JSON.parse(sessionStorage.getItem(key)!);
            if (val && val.id_token) return val.id_token;
          } catch { /* skip */ }
        }
      }
      // Check localStorage for dev token
      return localStorage.getItem('dev_auth_token');
    });

    if (idToken) {
      const tokenPath = path.join(authDir, 'google-token.json');
      fs.writeFileSync(tokenPath, JSON.stringify({ id_token: idToken }, null, 2));
      console.log(`🎫 ID token saved to: ${tokenPath}`);
    } else {
      console.warn('⚠️  Could not extract ID token. API-level tests may not work.');
    }

  } catch (error) {
    console.error('❌ Login timed out or failed. Please try again.');
    console.error('   Make sure you complete the Google login within 2 minutes.');
  } finally {
    await browser.close();
  }

  console.log('\n🎉 Done! You can now run Google auth E2E tests:');
  console.log('   npm run test:google');
}

main().catch(console.error);
