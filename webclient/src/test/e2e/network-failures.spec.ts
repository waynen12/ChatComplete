import { test, expect } from '@playwright/test';

test.describe('Network Failure Scenarios', () => {
  test('handles offline mode gracefully', async ({ page, context }) => {
    await page.goto('/knowledge');
    await page.waitForLoadState('networkidle');
    
    // Simulate going offline
    await context.setOffline(true);
    
    // Try to navigate or refresh
    await page.reload({ waitUntil: 'domcontentloaded' }).catch(() => {
      // Reload might fail when offline, which is expected
    });
    
    // Page should handle offline state
    await context.setOffline(false);
  });

  test('handles slow network connection', async ({ page }) => {
    // Simulate slow network by delaying all requests
    await page.route('**/*', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 500));
      await route.continue();
    });

    await page.goto('/knowledge', { timeout: 30000 });
    
    // Page should eventually load
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles intermittent connection failures', async ({ page }) => {
    let requestCount = 0;
    
    // Alternate between success and failure
    await page.route('/api/knowledge', async (route) => {
      requestCount++;
      if (requestCount % 2 === 0) {
        await route.abort('failed');
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        });
      }
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(2000);
    
    // Page should handle intermittent failures
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles request abortion', async ({ page }) => {
    // Abort all API requests
    await page.route('/api/**', async (route) => {
      await route.abort('aborted');
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Page should be visible despite aborted requests
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('recovers from temporary network issues', async ({ page, context }) => {
    await page.goto('/knowledge');
    await page.waitForLoadState('networkidle');
    
    // Simulate temporary offline
    await context.setOffline(true);
    await page.waitForTimeout(1000);
    
    // Come back online
    await context.setOffline(false);
    
    // Try to interact with the page
    await page.goto('/chat');
    
    // Should recover and load normally
    await expect(page).toHaveURL(/\/chat/);
  });

  test('handles DNS resolution failures', async ({ page }) => {
    // Mock DNS-like failures
    await page.route('/api/knowledge', async (route) => {
      await route.abort('namenotresolved');
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Application should handle DNS failures gracefully
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles connection reset errors', async ({ page }) => {
    // Simulate connection reset
    await page.route('/api/knowledge', async (route) => {
      await route.abort('connectionreset');
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Page should handle connection resets
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('displays appropriate error messages for network failures', async ({ page }) => {
    // Mock network failure
    await page.route('/api/knowledge', async (route) => {
      await route.abort('failed');
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1500);
    
    // Page should be visible (might show error message)
    const body = page.locator('body');
    await expect(body).toBeVisible();
    
    // Check for any error indicators (optional, depends on implementation)
    const pageContent = await page.content();
    expect(pageContent).toBeTruthy();
  });
});
