import { test, expect } from '@playwright/test';

test.describe('Landing Page', () => {
  test('loads and displays main heading', async ({ page }) => {
    await page.goto('/');
    
    // Check if the page title is visible
    await expect(page.getByRole('heading', { name: /AI Knowledge Manager/i })).toBeVisible();
  });

  test('displays CTA button and navigates to knowledge page', async ({ page }) => {
    await page.goto('/');
    
    // Check if the "Manage Knowledge" button is visible
    const ctaButton = page.getByRole('link', { name: /Manage Knowledge/i });
    await expect(ctaButton).toBeVisible();
    
    // Click the button and verify navigation
    await ctaButton.click();
    await expect(page).toHaveURL(/\/knowledge/);
  });

  test('renders without console errors', async ({ page }) => {
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.goto('/');
    
    // Wait a bit for any async operations
    await page.waitForTimeout(1000);
    
    // Verify no console errors
    expect(consoleErrors).toHaveLength(0);
  });
});
