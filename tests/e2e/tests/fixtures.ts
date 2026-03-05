import { test as base, Page } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Extended Playwright test fixture that captures JavaScript errors, console errors,
 * and failed requests during test execution.
 * 
 * Each test automatically generates a JSON artifact with all captured errors.
 * 
 * Usage:
 *   import { test, expect } from './fixtures';
 *   
 *   test('my test', async ({ page }) => {
 *     // JS errors, console errors, and failed requests are captured automatically
 *     await page.goto('/');
 *   });
 */

interface ErrorReport {
  testName: string;
  testFile: string;
  timestamp: string;
  errors: {
    pageErrors: Array<{ message: string; stack?: string; timestamp: string }>;
    consoleErrors: Array<{ text: string; timestamp: string }>;
    failedRequests: Array<{ url: string; method: string; status: number; timestamp: string }>;
  };
}

/**
 * Extend Playwright's base test with automatic error capture on the page fixture.
 */
export const test = base.extend<{}>({
  page: async ({ page }, use, testInfo) => {
    const errorReport: ErrorReport = {
      testName: testInfo.title,
      testFile: path.basename(testInfo.file),
      timestamp: new Date().toISOString(),
      errors: {
        pageErrors: [],
        consoleErrors: [],
        failedRequests: [],
      },
    };

    // Capture page errors (uncaught exceptions, unhandled promise rejections)
    page.on('pageerror', (error) => {
      errorReport.errors.pageErrors.push({
        message: error.message,
        stack: error.stack,
        timestamp: new Date().toISOString(),
      });
      console.error(`❌ [Page Error in "${testInfo.title}"] ${error.message}`);
    });

    // Capture console errors and warnings
    page.on('console', (msg) => {
      if (msg.type() === 'error' || msg.type() === 'warning') {
        errorReport.errors.consoleErrors.push({
          text: msg.text(),
          timestamp: new Date().toISOString(),
        });
        console.error(`❌ [Console ${msg.type()} in "${testInfo.title}"] ${msg.text()}`);
      }
    });

    // Capture failed network requests (4xx, 5xx)
    page.on('requestfailed', (request) => {
      const response = request.failure();
      errorReport.errors.failedRequests.push({
        url: request.url(),
        method: request.method(),
        status: 0, // Request failed before response
        timestamp: new Date().toISOString(),
      });
      console.error(`❌ [Request Failed in "${testInfo.title}"] ${request.method()} ${request.url()} - ${response?.errorText}`);
    });

    page.on('response', (response) => {
      if (response.status() >= 400) {
        errorReport.errors.failedRequests.push({
          url: response.url(),
          method: response.request().method(),
          status: response.status(),
          timestamp: new Date().toISOString(),
        });
        console.error(`❌ [HTTP ${response.status()} in "${testInfo.title}"] ${response.request().method()} ${response.url()}`);
      }
    });

    // Run the test
    await use(page);

    // After test completes, write error report JSON
    const hasErrors =
      errorReport.errors.pageErrors.length > 0 ||
      errorReport.errors.consoleErrors.length > 0 ||
      errorReport.errors.failedRequests.length > 0;

    if (hasErrors || testInfo.status !== 'passed') {
      const outputDir = path.join(testInfo.project.outputDir, 'error-reports');
      if (!fs.existsSync(outputDir)) {
        fs.mkdirSync(outputDir, { recursive: true });
      }

      const sanitizedTestName = testInfo.title.replace(/[^a-z0-9]/gi, '_');
      const reportPath = path.join(outputDir, `${sanitizedTestName}_${Date.now()}.json`);
      
      fs.writeFileSync(reportPath, JSON.stringify(errorReport, null, 2), 'utf-8');
      
      console.log(`📄 Error report saved: ${reportPath}`);
      testInfo.attach('error-report', {
        path: reportPath,
        contentType: 'application/json',
      });
    }
  },
});

export { expect } from '@playwright/test';
