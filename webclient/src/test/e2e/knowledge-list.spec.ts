import { test, expect } from '@playwright/test';
import { mockKnowledgeBases } from './helpers/api-mocks';

test.describe('Knowledge List Page', () => {
  test('loads knowledge list page', async ({ page }) => {
    await mockKnowledgeBases(page);
    await page.goto('/knowledge');
    
    // Check if the page loaded
    await expect(page).toHaveURL(/\/knowledge/);
  });

  test('displays page heading', async ({ page }) => {
    await mockKnowledgeBases(page);
    await page.goto('/knowledge');
    
    // Check for heading or navigation element
    const heading = page.getByRole('heading', { name: /knowledge/i }).first();
    await expect(heading).toBeVisible();
  });

  test('renders without console errors', async ({ page }) => {
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await mockKnowledgeBases(page);
    await page.goto('/knowledge');
    
    // Wait a bit for any async operations
    await page.waitForTimeout(1000);
    
    // Verify no console errors
    expect(consoleErrors).toHaveLength(0);
  });
});
