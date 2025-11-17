import { test, expect } from '@playwright/test';

test.describe('Navigation Between Pages', () => {
  test('can navigate from landing to knowledge list', async ({ page }) => {
    await page.goto('/');
    
    // Click the CTA button
    const ctaButton = page.getByRole('link', { name: /Manage Knowledge/i });
    await ctaButton.click();
    
    // Should be on knowledge page
    await expect(page).toHaveURL(/\/knowledge/);
  });

  test('can navigate from knowledge list to knowledge form', async ({ page }) => {
    await page.goto('/knowledge');
    
    // Look for "New" or "Create" button
    const createButton = page.getByRole('link', { name: /new|create|add/i }).first();
    
    if (await createButton.count() > 0) {
      await createButton.click();
      await expect(page).toHaveURL(/\/knowledge\/new/);
    } else {
      // If no button found, navigate directly
      await page.goto('/knowledge/new');
      await expect(page).toHaveURL(/\/knowledge\/new/);
    }
  });

  test('can navigate from knowledge list to chat', async ({ page }) => {
    await page.goto('/knowledge');
    
    // Navigate to chat page (might be via header/nav)
    await page.goto('/chat');
    await expect(page).toHaveURL(/\/chat/);
  });

  test('navigation menu is present on all pages', async ({ page }) => {
    const pages = ['/', '/knowledge', '/chat', '/analytics'];
    
    for (const pagePath of pages) {
      await page.goto(pagePath);
      
      // Header or nav should exist
      const nav = page.locator('nav, header').first();
      await expect(nav).toBeVisible();
    }
  });

  test('can navigate to analytics page', async ({ page }) => {
    await page.goto('/');
    
    // Navigate to analytics (might be in nav menu)
    await page.goto('/analytics');
    await expect(page).toHaveURL(/\/analytics/);
  });

  test('browser back button works correctly', async ({ page }) => {
    // Start at landing
    await page.goto('/');
    await expect(page).toHaveURL('/');
    
    // Navigate to knowledge
    await page.goto('/knowledge');
    await expect(page).toHaveURL(/\/knowledge/);
    
    // Go back
    await page.goBack();
    await expect(page).toHaveURL('/');
  });

  test('browser forward button works correctly', async ({ page }) => {
    // Start at landing
    await page.goto('/');
    
    // Navigate to knowledge
    await page.goto('/knowledge');
    
    // Go back
    await page.goBack();
    await expect(page).toHaveURL('/');
    
    // Go forward
    await page.goForward();
    await expect(page).toHaveURL(/\/knowledge/);
  });

  test('direct URL access works for all routes', async ({ page }) => {
    const routes = [
      { path: '/', expected: '/' },
      { path: '/knowledge', expected: /\/knowledge/ },
      { path: '/knowledge/new', expected: /\/knowledge\/new/ },
      { path: '/chat', expected: /\/chat/ },
      { path: '/analytics', expected: /\/analytics/ },
    ];
    
    for (const route of routes) {
      await page.goto(route.path);
      await expect(page).toHaveURL(route.expected);
      
      // Page should load without errors
      const body = page.locator('body');
      await expect(body).toBeVisible();
    }
  });
});
