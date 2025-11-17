import { test, expect } from '@playwright/test';

test.describe('Chat Page', () => {
  test('loads chat page', async ({ page }) => {
    await page.goto('/chat');
    
    // Check if the page loaded
    await expect(page).toHaveURL(/\/chat/);
  });

  test('displays chat interface elements', async ({ page }) => {
    await page.goto('/chat');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check for textarea or input element (chat input)
    const chatInput = page.getByRole('textbox').first();
    await expect(chatInput).toBeVisible();
  });

  test('renders without console errors', async ({ page }) => {
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.goto('/chat');
    
    // Wait a bit for any async operations
    await page.waitForTimeout(2000);
    
    // Verify no console errors (allowing for expected warnings)
    expect(consoleErrors).toHaveLength(0);
  });
});
