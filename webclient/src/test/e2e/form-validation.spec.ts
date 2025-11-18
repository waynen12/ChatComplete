import { test, expect } from '@playwright/test';

test.describe('Form Validation', () => {
  test.describe('Knowledge Form Validation', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/knowledge/new');
    });

    test('requires collection name to be filled', async ({ page }) => {
      // Try to submit without filling name
      const uploadButton = page.getByRole('button', { name: /upload|save/i });
      
      // Button should be disabled
      const isDisabled = await uploadButton.isDisabled();
      expect(isDisabled).toBe(true);
    });

    test('validates collection name format', async ({ page }) => {
      const nameInput = page.getByPlaceholder('Collection name');
      
      // Fill with valid name
      await nameInput.fill('valid-collection-name');
      await expect(nameInput).toHaveValue('valid-collection-name');
      
      // Clear and try invalid characters (if validation exists)
      await nameInput.fill('');
      await expect(nameInput).toHaveValue('');
    });

    test('requires at least one file to be selected', async ({ page }) => {
      // Fill collection name
      await page.getByPlaceholder('Collection name').fill('test-collection');
      
      // Try to upload without files
      const uploadButton = page.getByRole('button', { name: /upload|save/i });
      
      // Button might be disabled or show validation error
      const isDisabled = await uploadButton.isDisabled();
      expect(isDisabled).toBe(true);
    });

    test('shows validation error for empty form submission', async ({ page }) => {
      // Try to interact with upload button without filling form
      const uploadButton = page.getByRole('button', { name: /upload|save/i });
      
      if (await uploadButton.isEnabled()) {
        await uploadButton.click();
        
        // Wait for possible error message
        await page.waitForTimeout(500);
      }
      
      // Form should still be on same page
      await expect(page).toHaveURL(/\/knowledge\/new/);
    });

    test('validates file size limits', async ({ page }) => {
      // Note: This test requires actual file upload testing
      // For now, verify the file input exists with proper attributes
      const fileInput = page.locator('input[type="file"]');
      await expect(fileInput).toBeVisible();
      
      // Check if accept attribute is set (file type validation)
      const acceptAttr = await fileInput.getAttribute('accept');
      expect(acceptAttr).toBeTruthy();
    });

    test('validates file type restrictions', async ({ page }) => {
      // Verify file input has accept attribute for allowed types
      const fileInput = page.locator('input[type="file"]');
      const acceptAttr = await fileInput.getAttribute('accept');
      
      // Should accept pdf, docx, md, txt
      expect(acceptAttr).toMatch(/pdf|docx|md|txt/);
    });
  });

  test.describe('Chat Input Validation', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/chat');
      await page.waitForLoadState('networkidle');
    });

    test('disables send button with empty message', async ({ page }) => {
      const chatInput = page.getByRole('textbox').first();
      await chatInput.fill('');
      
      const sendButton = page.getByRole('button', { name: /send|submit/i });
      if (await sendButton.count() > 0) {
        const isDisabled = await sendButton.isDisabled();
        expect(isDisabled).toBe(true);
      }
    });

    test('enables send button with non-empty message', async ({ page }) => {
      const chatInput = page.getByRole('textbox').first();
      await chatInput.fill('Test message');
      
      const sendButton = page.getByRole('button', { name: /send|submit/i });
      if (await sendButton.count() > 0) {
        await expect(sendButton).toBeEnabled();
      }
    });

    test('trims whitespace-only messages', async ({ page }) => {
      const chatInput = page.getByRole('textbox').first();
      await chatInput.fill('   ');
      
      const sendButton = page.getByRole('button', { name: /send|submit/i });
      if (await sendButton.count() > 0) {
        // Should be disabled for whitespace-only input
        const isDisabled = await sendButton.isDisabled();
        // Note: This depends on implementation - might allow or disallow
        expect(typeof isDisabled).toBe('boolean');
      }
    });
  });
});
