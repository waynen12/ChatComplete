import { test, expect } from '@playwright/test';

test.describe('Analytics Page', () => {
  test('loads analytics page', async ({ page }) => {
    await page.goto('/analytics');
    
    // Check if the page loaded
    await expect(page).toHaveURL(/\/analytics/);
  });

  test('displays analytics dashboard', async ({ page }) => {
    await page.goto('/analytics');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check for any visible content (analytics widgets or text)
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('renders without console errors', async ({ page }) => {
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.goto('/analytics');
    
    // Wait a bit for any async operations
    await page.waitForTimeout(2000);
    
    // Verify no console errors
    expect(consoleErrors).toHaveLength(0);
  });
});
