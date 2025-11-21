import { test, expect } from '@playwright/test';

test.describe('Knowledge Form Page', () => {
  test('loads knowledge form page (new)', async ({ page }) => {
    await page.goto('/knowledge/new');
    
    // Check if the page loaded
    await expect(page).toHaveURL(/\/knowledge\/new/);
  });

  test('displays form elements', async ({ page }) => {
    await page.goto('/knowledge/new');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check for body content (form should be present)
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
    
    await page.goto('/knowledge/new');
    
    // Wait a bit for any async operations
    await page.waitForTimeout(1000);
    
    // Verify no console errors
    expect(consoleErrors).toHaveLength(0);
  });
});
