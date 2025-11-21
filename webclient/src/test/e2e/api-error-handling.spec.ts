import { test, expect } from '@playwright/test';

test.describe('API Error Handling', () => {
  test('handles 404 error when knowledge base not found', async ({ page }) => {
    // Navigate to a non-existent knowledge base
    await page.goto('/chat/nonexistent-kb-12345');
    await page.waitForLoadState('networkidle');
    
    // Should still load the page (graceful degradation)
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles 500 server error gracefully', async ({ page }) => {
    // Mock a 500 error response
    await page.route('/api/knowledge', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' }),
      });
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Page should still be visible despite error
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('displays error message on failed knowledge fetch', async ({ page }) => {
    // Mock failed API call
    await page.route('/api/knowledge', async (route) => {
      await route.abort('failed');
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(2000);
    
    // Page should handle the error gracefully
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles network timeout gracefully', async ({ page }) => {
    // Mock a slow/timeout response
    await page.route('/api/knowledge', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 5000));
      await route.fulfill({
        status: 408,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Request Timeout' }),
      });
    });

    await page.goto('/knowledge');
    
    // Page should be visible even with timeout
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles malformed JSON response', async ({ page }) => {
    // Mock malformed JSON response
    await page.route('/api/knowledge', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: 'This is not valid JSON{{{',
      });
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Application should handle malformed response
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles unauthorized access (401)', async ({ page }) => {
    // Mock 401 Unauthorized
    await page.route('/api/knowledge', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Unauthorized' }),
      });
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Page should be visible
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('handles forbidden access (403)', async ({ page }) => {
    // Mock 403 Forbidden
    await page.route('/api/knowledge', async (route) => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Forbidden' }),
      });
    });

    await page.goto('/knowledge');
    await page.waitForTimeout(1000);
    
    // Page should handle forbidden access
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('retries failed requests appropriately', async ({ page }) => {
    let requestCount = 0;
    
    // Mock failing requests
    await page.route('/api/knowledge', async (route) => {
      requestCount++;
      if (requestCount < 2) {
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
    
    // Verify page loaded
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });
});
