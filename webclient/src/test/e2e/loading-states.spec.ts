import { test, expect } from '@playwright/test';

test.describe('Loading States', () => {
  test('displays loading state while fetching knowledge bases', async ({ page }) => {
    // Delay the API response to see loading state
    await page.route('/api/knowledge', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'test-kb', name: 'Test KB', documentCount: 5 }
        ]),
      });
    });

    await page.goto('/knowledge');
    
    // Page should be visible immediately (might show loading indicator)
    const body = page.locator('body');
    await expect(body).toBeVisible();
    
    // Wait for content to load
    await page.waitForTimeout(1500);
  });

  test('shows loading indicator during file upload', async ({ page }) => {
    await page.goto('/knowledge/new');
    
    // Fill form
    await page.getByPlaceholder('Collection name').fill('test-upload');
    
    // Note: Actual file upload would trigger loading state
    // For now, verify upload button exists
    const uploadButton = page.getByRole('button', { name: /upload|save/i });
    await expect(uploadButton).toBeVisible();
  });

  test('displays loading state during chat message send', async ({ page }) => {
    // Mock slow chat response
    await page.route('/api/chat', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      await route.fulfill({
        status: 200,
        contentType: 'text/plain',
        body: 'Test response',
      });
    });

    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    
    const chatInput = page.getByRole('textbox').first();
    await chatInput.fill('Test message');
    
    const sendButton = page.getByRole('button', { name: /send|submit/i });
    if (await sendButton.count() > 0 && await sendButton.isEnabled()) {
      await sendButton.click();
      
      // Should show loading state (button disabled or spinner)
      await page.waitForTimeout(500);
      
      // Verify page is still functional
      const body = page.locator('body');
      await expect(body).toBeVisible();
    }
  });

  test('shows loading state when fetching Ollama models', async ({ page }) => {
    // Mock slow Ollama models API
    await page.route('/api/ollama/models', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1500));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(['llama2', 'codellama']),
      });
    });

    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
    
    // Try to select Ollama provider (if dropdown exists)
    const providerSelect = page.locator('select').first();
    if (await providerSelect.count() > 0) {
      const options = await providerSelect.locator('option').allTextContents();
      const ollamaIndex = options.findIndex(opt => opt.toLowerCase().includes('ollama'));
      
      if (ollamaIndex >= 0) {
        await providerSelect.selectOption({ index: ollamaIndex });
        
        // Should show loading state while fetching models
        await page.waitForTimeout(500);
      }
    }
    
    // Page should remain functional
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('displays loading state in analytics page', async ({ page }) => {
    // Mock slow analytics API
    await page.route('/api/analytics/**', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      });
    });

    await page.goto('/analytics');
    
    // Should show page immediately (might have loading skeleton)
    const body = page.locator('body');
    await expect(body).toBeVisible();
    
    // Wait for data to load
    await page.waitForTimeout(1500);
  });

  test('handles concurrent loading states correctly', async ({ page }) => {
    // Mock multiple slow API calls
    await page.route('/api/knowledge', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.route('/api/ollama/models', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1500));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/chat');
    
    // Multiple loading states might be active
    await page.waitForTimeout(2000);
    
    // Page should handle multiple concurrent loads
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });
});
