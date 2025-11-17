# Playwright-MCP Quick Start Guide

**Target Audience:** Developers implementing Playwright-MCP tests  
**Time to Read:** 10 minutes  
**Companion Document:** [PLAYWRIGHT_MCP_TESTING_REPORT.md](./PLAYWRIGHT_MCP_TESTING_REPORT.md)

---

## Setup in 5 Minutes

### Step 1: Install Playwright

```bash
cd webclient
npm install -D @playwright/test
npx playwright install chromium
```

### Step 2: Create Playwright Config

```typescript
// playwright.config.ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './src/test/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 4,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
  },
});
```

### Step 3: Write Your First Test

```typescript
// src/test/e2e/landing-page.spec.ts
import { test, expect } from '@playwright/test';

test('landing page loads', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /AI Knowledge Manager/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /Manage Knowledge/i })).toBeVisible();
});

test('navigate to knowledge page', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: /Manage Knowledge/i }).click();
  await expect(page).toHaveURL(/\/knowledge/);
  await expect(page.getByText(/Knowledge Bases/i)).toBeVisible();
});
```

### Step 4: Run Tests

```bash
# Run all tests
npx playwright test

# Run specific test file
npx playwright test landing-page.spec.ts

# Run in UI mode (interactive)
npx playwright test --ui

# Generate report
npx playwright show-report
```

---

## Test Template Library

### Template 1: Basic Page Test

```typescript
import { test, expect } from '@playwright/test';

test.describe('Page Name', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/path');
  });

  test('renders correctly', async ({ page }) => {
    await expect(page.getByRole('heading')).toBeVisible();
  });

  test('handles navigation', async ({ page }) => {
    await page.getByRole('button', { name: 'Click Me' }).click();
    await expect(page).toHaveURL(/\/new-path/);
  });
});
```

### Template 2: Form Submission Test

```typescript
test('submits form successfully', async ({ page }) => {
  await page.goto('/knowledge/upload');
  
  // Fill form
  await page.getByLabel('Knowledge ID').fill('test-kb');
  
  // Upload file
  const fileInput = page.locator('input[type="file"]');
  await fileInput.setInputFiles('./fixtures/test.pdf');
  
  // Submit
  await page.getByRole('button', { name: 'Upload' }).click();
  
  // Verify success
  await expect(page.getByText(/Upload successful/i)).toBeVisible();
});
```

### Template 3: API Integration Test

```typescript
test('loads data from API', async ({ page }) => {
  // Intercept API call
  await page.route('/api/knowledge', async (route) => {
    await route.fulfill({
      status: 200,
      body: JSON.stringify([
        { id: 'test-kb', name: 'Test Knowledge Base', documentCount: 5 }
      ]),
    });
  });

  await page.goto('/knowledge');
  
  // Verify data displayed
  await expect(page.getByText('Test Knowledge Base')).toBeVisible();
  await expect(page.getByText('5 documents')).toBeVisible();
});
```

### Template 4: Error Handling Test

```typescript
test('displays error message on API failure', async ({ page }) => {
  // Mock API error
  await page.route('/api/chat', async (route) => {
    await route.fulfill({
      status: 500,
      body: JSON.stringify({ error: 'Internal Server Error' }),
    });
  });

  await page.goto('/chat');
  await page.getByLabel('Chat input').fill('Test message');
  await page.getByRole('button', { name: 'Send' }).click();
  
  // Verify error message
  await expect(page.getByText(/Something went wrong/i)).toBeVisible();
});
```

### Template 5: Accessibility Test

```typescript
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('has no accessibility violations', async ({ page }) => {
  await page.goto('/');
  
  const accessibilityScanResults = await new AxeBuilder({ page }).analyze();
  
  expect(accessibilityScanResults.violations).toEqual([]);
});

test('keyboard navigation works', async ({ page }) => {
  await page.goto('/chat');
  
  // Tab to input
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  
  // Verify focus
  await expect(page.getByLabel('Chat input')).toBeFocused();
  
  // Type and submit with Enter
  await page.keyboard.type('Test message');
  await page.keyboard.press('Enter');
  
  await expect(page.getByText('Test message')).toBeVisible();
});
```

### Template 6: Responsive Test

```typescript
test('is mobile responsive', async ({ page, viewport }) => {
  // Desktop
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto('/chat');
  await expect(page.getByRole('navigation')).toBeVisible();
  
  // Mobile
  await page.setViewportSize({ width: 375, height: 667 });
  await expect(page.getByRole('navigation')).not.toBeVisible(); // Hamburger menu instead
  await expect(page.getByRole('button', { name: 'Menu' })).toBeVisible();
});
```

---

## Priority Test Checklist

### P0: Critical (Start Here) 🔴

- [ ] Landing page loads
- [ ] Navigation between pages works
- [ ] Knowledge base creation
- [ ] File upload (basic)
- [ ] Chat message send/receive
- [ ] Provider selection

**Est. Time:** 8 hours  
**Impact:** Prevents critical production bugs

### P1: High (Week 1) 🟠

- [ ] Form validation
- [ ] Error message display
- [ ] Loading states
- [ ] API error handling
- [ ] Dark mode toggle
- [ ] Mobile navigation

**Est. Time:** 12 hours  
**Impact:** Improves user experience quality

### P2: Medium (Week 2-3) 🟡

- [ ] Chat history persistence
- [ ] Analytics dashboard
- [ ] Ollama model management
- [ ] Keyboard shortcuts
- [ ] Responsive layouts

**Est. Time:** 16 hours  
**Impact:** Comprehensive feature coverage

### P3: Low (Ongoing) 🟢

- [ ] Visual regression
- [ ] Performance benchmarks
- [ ] Accessibility audits
- [ ] Cross-browser testing

**Est. Time:** 10 hours  
**Impact:** Polish and maintainability

---

## Common Patterns

### Pattern 1: Test Data Setup

```typescript
// helpers/test-data.ts
export class TestDataManager {
  static generateKnowledgeId(): string {
    return `test-kb-${Date.now()}`;
  }

  static async createTestKnowledge(id: string): Promise<void> {
    await fetch('/api/knowledge', {
      method: 'POST',
      body: createTestFormData(id),
    });
  }

  static async cleanupTestKnowledge(id: string): Promise<void> {
    await fetch(`/api/knowledge/${id}`, { method: 'DELETE' });
  }
}

// Usage in test
test('uses test knowledge base', async ({ page }) => {
  const kbId = TestDataManager.generateKnowledgeId();
  await TestDataManager.createTestKnowledge(kbId);
  
  try {
    await page.goto(`/chat/${kbId}`);
    // ... test logic
  } finally {
    await TestDataManager.cleanupTestKnowledge(kbId);
  }
});
```

### Pattern 2: Page Object Model

```typescript
// pages/ChatPage.ts
export class ChatPage {
  constructor(private page: Page) {}

  async goto(knowledgeId?: string) {
    const url = knowledgeId ? `/chat/${knowledgeId}` : '/chat';
    await this.page.goto(url);
  }

  async selectKnowledgeBase(name: string) {
    await this.page.getByLabel('Knowledge base').selectOption(name);
  }

  async sendMessage(message: string) {
    await this.page.getByLabel('Chat input').fill(message);
    await this.page.getByRole('button', { name: 'Send' }).click();
  }

  async waitForResponse() {
    await this.page.waitForSelector('[data-testid="assistant-message"]');
  }
}

// Usage
test('chat flow', async ({ page }) => {
  const chatPage = new ChatPage(page);
  await chatPage.goto('test-kb');
  await chatPage.selectKnowledgeBase('test-kb');
  await chatPage.sendMessage('Hello');
  await chatPage.waitForResponse();
});
```

### Pattern 3: Custom Fixtures

```typescript
// fixtures/test-fixtures.ts
import { test as base } from '@playwright/test';
import { TestDataManager } from './test-data';

type TestFixtures = {
  testKnowledgeId: string;
};

export const test = base.extend<TestFixtures>({
  testKnowledgeId: async ({}, use) => {
    const id = TestDataManager.generateKnowledgeId();
    await TestDataManager.createTestKnowledge(id);
    await use(id);
    await TestDataManager.cleanupTestKnowledge(id);
  },
});

// Usage
test('uses auto-cleanup knowledge base', async ({ page, testKnowledgeId }) => {
  await page.goto(`/chat/${testKnowledgeId}`);
  // Test automatically cleans up after
});
```

---

## Debugging Tips

### Tip 1: Run in Headed Mode

```bash
npx playwright test --headed
```

### Tip 2: Use Debug Mode

```bash
npx playwright test --debug
```

### Tip 3: Add Console Logs

```typescript
test('debug test', async ({ page }) => {
  page.on('console', msg => console.log('PAGE LOG:', msg.text()));
  await page.goto('/');
});
```

### Tip 4: Pause Execution

```typescript
test('pause for inspection', async ({ page }) => {
  await page.goto('/');
  await page.pause(); // Opens Playwright Inspector
});
```

### Tip 5: Take Screenshots

```typescript
test('screenshot on failure', async ({ page }) => {
  try {
    await page.goto('/');
    // ... test logic
  } catch (error) {
    await page.screenshot({ path: 'failure.png', fullPage: true });
    throw error;
  }
});
```

---

## CI/CD Integration

### Add to package.json

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug"
  }
}
```

### GitHub Actions

```yaml
# .github/workflows/playwright.yml
name: Playwright Tests

on:
  push:
    branches: [main]
  pull_request:

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 20
      - name: Install dependencies
        run: npm ci
      - name: Install Playwright
        run: npx playwright install --with-deps chromium
      - name: Run tests
        run: npm run test:e2e
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
```

---

## Next Steps

1. **Install Playwright** (5 min)
2. **Copy test templates** (10 min)
3. **Write 3 critical tests** (2 hours)
4. **Run tests locally** (5 min)
5. **Add to CI/CD** (30 min)
6. **Iterate and expand** (ongoing)

---

## Resources

- **Full Report:** [PLAYWRIGHT_MCP_TESTING_REPORT.md](./PLAYWRIGHT_MCP_TESTING_REPORT.md)
- **Playwright Docs:** https://playwright.dev/
- **Best Practices:** https://playwright.dev/docs/best-practices
- **Examples:** https://github.com/microsoft/playwright/tree/main/examples

---

**Last Updated:** November 17, 2025  
**Maintained By:** Development Team
