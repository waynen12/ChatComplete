# Playwright-MCP UI Testing Coverage Report

**Date:** November 17, 2025  
**Project:** ChatComplete - AI Knowledge Manager  
**Purpose:** Comprehensive strategy for using Playwright-MCP to increase UI test coverage

---

## Executive Summary

This report provides a comprehensive analysis of how to use **Playwright-MCP** (Model Context Protocol integration with Playwright) to dramatically increase UI test coverage for the ChatComplete application. Currently, the application has minimal UI testing (1 basic smoke test), leaving critical user flows untested. This document outlines a complete testing strategy, specific test scenarios, implementation examples, and best practices.

### Key Findings:
- **Current State:** Only 1 basic unit test exists; no E2E or integration tests
- **Coverage Gap:** 0% of critical user flows are tested
- **Recommended Coverage:** 85%+ of critical paths should be tested with Playwright-MCP
- **Estimated Impact:** 50+ new test scenarios covering all 6 pages and key workflows

---

## Table of Contents

1. [What is Playwright-MCP?](#what-is-playwright-mcp)
2. [Current Testing State](#current-testing-state)
3. [Playwright-MCP Capabilities](#playwright-mcp-capabilities)
4. [Testing Strategy](#testing-strategy)
5. [Page-by-Page Test Coverage Plan](#page-by-page-test-coverage-plan)
6. [Critical User Flow Tests](#critical-user-flow-tests)
7. [Implementation Examples](#implementation-examples)
8. [Test Organization & Structure](#test-organization--structure)
9. [Best Practices](#best-practices)
10. [Integration with CI/CD](#integration-with-cicd)
11. [Maintenance & Monitoring](#maintenance--monitoring)
12. [Recommended Implementation Roadmap](#recommended-implementation-roadmap)

---

## What is Playwright-MCP?

**Playwright-MCP** is an integration of Playwright browser automation with the Model Context Protocol (MCP), enabling AI-assisted browser testing. It combines:

- **Playwright's** powerful browser automation capabilities
- **MCP's** standardized protocol for AI-tool communication
- **Accessibility-first** testing approach using semantic snapshots

### Key Advantages:

1. **Accessibility Testing Built-in**: Uses accessibility tree snapshots for element selection
2. **AI-Friendly**: Works seamlessly with AI coding assistants like GitHub Copilot
3. **Human-Readable**: Test code is more intuitive and maintainable
4. **Cross-Browser**: Tests run on Chromium, Firefox, and WebKit
5. **Visual Regression**: Built-in screenshot capabilities
6. **Network Monitoring**: Track API calls and performance
7. **JavaScript Evaluation**: Execute custom scripts in browser context

---

## Current Testing State

### Existing Tests:
```
webclient/src/test/
├── App.test.tsx       # 1 basic smoke test
└── setup.ts           # Vitest configuration
```

**Current Coverage:**
- ✅ Basic component rendering (App)
- ❌ Page-level functionality (0/6 pages)
- ❌ User interactions (forms, buttons, navigation)
- ❌ API integration
- ❌ Real-time features (SignalR)
- ❌ Accessibility compliance
- ❌ Cross-browser compatibility
- ❌ Mobile responsiveness
- ❌ Performance testing

**Gap Analysis:**
- **Critical Gap:** No end-to-end tests for core workflows (upload → chat → analytics)
- **Risk:** Production bugs in UI/UX interactions
- **Impact:** Manual testing required for every release

---

## Playwright-MCP Capabilities

### Available Tools:

#### 1. Navigation & Page Interaction
- `browser_navigate(url)` - Navigate to pages
- `browser_snapshot()` - Capture accessibility tree (better than screenshots for testing)
- `browser_take_screenshot()` - Visual regression testing
- `browser_wait_for(text)` - Wait for content to appear
- `browser_tabs()` - Multi-tab testing

#### 2. User Input Simulation
- `browser_click(element, ref)` - Click buttons, links
- `browser_type(element, ref, text)` - Type in inputs
- `browser_fill_form(fields)` - Batch form filling
- `browser_select_option(element, ref, values)` - Dropdown selection
- `browser_press_key(key)` - Keyboard shortcuts

#### 3. Advanced Interactions
- `browser_hover(element, ref)` - Hover states
- `browser_drag(startElement, endElement)` - Drag-and-drop (critical for Analytics page!)
- `browser_handle_dialog(accept)` - Handle alerts/confirms

#### 4. Testing & Debugging
- `browser_evaluate(function)` - Execute JavaScript
- `browser_network_requests()` - Monitor API calls
- `browser_console_messages()` - Track console errors
- `browser_resize(width, height)` - Responsive testing

### Why These Tools Matter for ChatComplete:

| Feature | Playwright-MCP Tool | Use Case |
|---------|---------------------|----------|
| Knowledge Upload | `browser_fill_form`, `browser_click` | Test file upload flow |
| Chat Interface | `browser_type`, `browser_wait_for` | Test message sending/receiving |
| Analytics Drag-and-Drop | `browser_drag` | Test dashboard customization |
| Provider Selection | `browser_select_option` | Test LLM provider switching |
| Real-time Updates | `browser_wait_for`, `browser_network_requests` | Test SignalR updates |
| Mobile Experience | `browser_resize` | Test responsive layouts |
| Accessibility | `browser_snapshot` | Test ARIA labels, keyboard nav |

---

## Testing Strategy

### Pyramid Approach:

```
           ┌─────────────┐
           │  Manual     │  5% - Exploratory testing
           └─────────────┘
          ┌───────────────┐
          │ E2E (Playwright)│ 15% - Critical user flows
          └───────────────┘
        ┌───────────────────┐
        │  Integration       │ 30% - API + UI integration
        └───────────────────┘
      ┌───────────────────────┐
      │    Unit (Vitest)       │ 50% - Component logic
      └───────────────────────┘
```

### Priority Levels:

#### 🔴 P0 - Critical (Must Have)
- Landing page loads
- Navigation between pages
- Knowledge base creation
- File upload
- Chat message send/receive
- Provider selection

#### 🟠 P1 - High (Should Have)
- Form validation
- Error handling
- Ollama model management
- Analytics dashboard loading
- Dark mode toggle
- Mobile responsiveness

#### 🟡 P2 - Medium (Nice to Have)
- Chat history persistence
- Analytics drag-and-drop
- Keyboard shortcuts
- Hover states
- Loading animations

#### 🟢 P3 - Low (Future)
- Performance benchmarks
- Visual regression
- Cross-browser quirks
- Accessibility audits

---

## Page-by-Page Test Coverage Plan

### 1. Landing Page (`LandingPage.tsx`)

**Priority:** 🔴 P0

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Page renders with title and CTA | Smoke | `browser_navigate`, `browser_snapshot` |
| "Manage Knowledge" button navigates | Navigation | `browser_click`, `browser_wait_for` |
| Gradient background displays | Visual | `browser_take_screenshot` |
| Responsive layout on mobile | Responsive | `browser_resize`, `browser_snapshot` |
| Keyboard navigation works | Accessibility | `browser_press_key`, `browser_snapshot` |

#### Example Test:
```typescript
test('Landing page loads and displays CTA', async () => {
  await browser_navigate('http://localhost:5173/');
  const snapshot = await browser_snapshot();
  
  // Verify page title
  expect(snapshot).toContain('AI Knowledge Manager');
  
  // Verify CTA button exists
  expect(snapshot).toContain('Manage Knowledge');
  
  // Test button click
  await browser_click('Manage Knowledge button', '<ref-from-snapshot>');
  await browser_wait_for({ text: 'Knowledge Bases' });
});
```

**Coverage Goal:** 5 tests

---

### 2. Knowledge List Page (`KnowledgeListPage.tsx`)

**Priority:** 🔴 P0

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Empty state displays with "Upload" CTA | Empty State | `browser_snapshot`, `browser_wait_for` |
| Knowledge bases load from API | Integration | `browser_network_requests` |
| Search/filter functionality | Interaction | `browser_type`, `browser_wait_for` |
| Create new knowledge base button | Navigation | `browser_click` |
| Delete knowledge base with confirmation | Interaction | `browser_click`, `browser_handle_dialog` |
| Pagination works (if applicable) | Interaction | `browser_click` |

#### Example Test:
```typescript
test('Empty state displays with upload CTA', async () => {
  await browser_navigate('http://localhost:5173/knowledge');
  const snapshot = await browser_snapshot();
  
  // Verify empty state message
  expect(snapshot).toContain('No knowledge bases yet');
  expect(snapshot).toContain('Upload your first document');
});

test('Lists knowledge bases from API', async () => {
  // Seed test data first (via API)
  await fetch('/api/knowledge', {
    method: 'POST',
    body: formData // with test document
  });
  
  await browser_navigate('http://localhost:5173/knowledge');
  await browser_wait_for({ text: 'test-knowledge-base' });
  
  const snapshot = await browser_snapshot();
  expect(snapshot).toContain('test-knowledge-base');
});
```

**Coverage Goal:** 8 tests

---

### 3. Knowledge Form Page (`KnowledgeFormPage.tsx`)

**Priority:** 🔴 P0

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Form renders with all fields | Smoke | `browser_snapshot` |
| File upload via drag-and-drop | Interaction | `browser_drag` (if supported), `browser_file_upload` |
| File upload via file picker | Interaction | `browser_click`, `browser_file_upload` |
| Knowledge ID validation | Validation | `browser_type`, `browser_wait_for` |
| Submit button disabled when invalid | Validation | `browser_snapshot` |
| Successful upload shows success message | Integration | `browser_fill_form`, `browser_click`, `browser_wait_for` |
| Upload progress indicator displays | UI Feedback | `browser_wait_for`, `browser_snapshot` |
| Error handling for failed uploads | Error Handling | `browser_network_requests`, `browser_wait_for` |

#### Example Test:
```typescript
test('File upload flow works end-to-end', async () => {
  await browser_navigate('http://localhost:5173/knowledge/upload');
  
  // Fill knowledge ID
  await browser_fill_form({
    fields: [
      { name: 'Knowledge ID', type: 'textbox', ref: '<ref>', value: 'test-kb' }
    ]
  });
  
  // Upload file (note: may need custom implementation)
  await browser_click('Upload files button', '<ref>');
  await browser_file_upload({ paths: ['/path/to/test.pdf'] });
  
  // Submit
  await browser_click('Upload button', '<ref>');
  
  // Verify success
  await browser_wait_for({ text: 'Upload successful' });
});
```

**Coverage Goal:** 10 tests

---

### 4. Chat Page (`ChatPage.tsx`)

**Priority:** 🔴 P0 (Most Complex)

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Chat page loads with knowledge base selector | Smoke | `browser_navigate`, `browser_snapshot` |
| Knowledge base selection works | Interaction | `browser_select_option` |
| Provider (OpenAI/Ollama/etc.) selection | Interaction | `browser_select_option` |
| Send message and receive response (mocked) | Integration | `browser_type`, `browser_click`, `browser_wait_for` |
| Settings panel opens/closes | Interaction | `browser_click`, `browser_snapshot` |
| Chat history persists | State | `browser_navigate`, `browser_snapshot` |
| Agent mode toggle | Interaction | `browser_click` |
| Message formatting (Markdown rendering) | Visual | `browser_snapshot`, `browser_take_screenshot` |
| Empty state displays helpful message | Empty State | `browser_snapshot` |
| Textarea auto-resizes on input | UI Behavior | `browser_type`, `browser_evaluate` |
| Submit button enabled/disabled state | Validation | `browser_snapshot` |
| SignalR real-time updates (if testable) | Real-time | `browser_network_requests`, `browser_wait_for` |
| Error handling for failed API calls | Error Handling | `browser_wait_for` |

#### Example Test:
```typescript
test('Send message and receive response', async () => {
  await browser_navigate('http://localhost:5173/chat/test-kb');
  
  // Select knowledge base
  await browser_select_option('Knowledge base dropdown', '<ref>', ['test-kb']);
  
  // Select provider
  await browser_select_option('Provider dropdown', '<ref>', ['Ollama']);
  
  // Type message
  await browser_type('Chat input textarea', '<ref>', 'What is this about?');
  
  // Send message
  await browser_click('Send button', '<ref>');
  
  // Wait for response
  await browser_wait_for({ text: 'Based on the documents' });
  
  const snapshot = await browser_snapshot();
  expect(snapshot).toContain('What is this about?'); // User message
  expect(snapshot).toContain('Based on the documents'); // AI response
});
```

**Coverage Goal:** 15 tests

---

### 5. Analytics Page (`AnalyticsPage.tsx`)

**Priority:** 🟠 P1

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Analytics page loads with widgets | Smoke | `browser_navigate`, `browser_snapshot` |
| KPIs display with correct data | Integration | `browser_network_requests`, `browser_snapshot` |
| Charts render (Recharts integration) | Visual | `browser_take_screenshot` |
| Drag-and-drop to reorder widgets | Interaction | `browser_drag` |
| Resize widgets (if supported) | Interaction | `browser_drag` |
| Export data functionality | Interaction | `browser_click`, `browser_network_requests` |
| Filter by date range | Interaction | `browser_select_option`, `browser_wait_for` |
| Real-time updates via SignalR | Real-time | `browser_wait_for` |
| Responsive grid layout | Responsive | `browser_resize`, `browser_snapshot` |

#### Example Test:
```typescript
test('Drag-and-drop to reorder widgets', async () => {
  await browser_navigate('http://localhost:5173/analytics');
  await browser_wait_for({ text: 'Analytics Dashboard' });
  
  const beforeSnapshot = await browser_snapshot();
  
  // Drag first widget to second position
  await browser_drag(
    'Usage Trends widget', '<start-ref>',
    'Provider Status widget', '<end-ref>'
  );
  
  const afterSnapshot = await browser_snapshot();
  
  // Verify widgets reordered
  expect(beforeSnapshot).not.toEqual(afterSnapshot);
});
```

**Coverage Goal:** 12 tests

---

### 6. Models Page (Ollama Management)

**Priority:** 🟠 P1

#### Test Scenarios:

| Scenario | Type | Playwright-MCP Tools |
|----------|------|----------------------|
| Models page loads with available models | Smoke | `browser_navigate`, `browser_snapshot` |
| Model download triggers progress indicator | Integration | `browser_click`, `browser_wait_for` |
| Model delete with confirmation | Interaction | `browser_click`, `browser_handle_dialog` |
| Model search/filter | Interaction | `browser_type`, `browser_wait_for` |
| Model details modal opens | Interaction | `browser_click`, `browser_snapshot` |

**Coverage Goal:** 7 tests

---

## Critical User Flow Tests

### Flow 1: Complete Knowledge Management Workflow 🔴 P0

**Steps:**
1. Navigate to landing page
2. Click "Manage Knowledge"
3. Create new knowledge base
4. Upload document
5. Verify success
6. Navigate to knowledge list
7. Verify new KB appears

**Playwright-MCP Implementation:**
```typescript
test('E2E: Complete knowledge upload workflow', async () => {
  // Step 1: Landing page
  await browser_navigate('http://localhost:5173/');
  await browser_click('Manage Knowledge button', '<ref>');
  
  // Step 2: Navigate to upload
  await browser_click('Create new knowledge base button', '<ref>');
  
  // Step 3: Fill form
  await browser_fill_form({
    fields: [
      { name: 'Knowledge ID', type: 'textbox', ref: '<ref>', value: 'e2e-test-kb' }
    ]
  });
  
  // Step 4: Upload file
  await browser_file_upload({ paths: ['/test-fixtures/sample.pdf'] });
  await browser_click('Upload button', '<ref>');
  
  // Step 5: Verify success
  await browser_wait_for({ text: 'Upload successful' });
  
  // Step 6: Go back to list
  await browser_navigate('http://localhost:5173/knowledge');
  
  // Step 7: Verify KB exists
  await browser_wait_for({ text: 'e2e-test-kb' });
});
```

---

### Flow 2: Chat with Knowledge Base 🔴 P0

**Steps:**
1. Navigate to chat page
2. Select knowledge base
3. Select provider
4. Send message
5. Receive response
6. Verify conversation history
7. Open settings panel
8. Change provider
9. Send another message

**Playwright-MCP Implementation:**
```typescript
test('E2E: Complete chat workflow', async () => {
  await browser_navigate('http://localhost:5173/chat');
  
  // Select KB
  await browser_select_option('Knowledge base dropdown', '<ref>', ['test-kb']);
  
  // Select provider
  await browser_select_option('Provider dropdown', '<ref>', ['Ollama']);
  
  // Send message
  await browser_type('Chat input', '<ref>', 'Summarize the document');
  await browser_click('Send button', '<ref>');
  
  // Wait for response
  await browser_wait_for({ text: 'Summary:' });
  
  // Verify history
  const snapshot = await browser_snapshot();
  expect(snapshot).toContain('Summarize the document');
  expect(snapshot).toContain('Summary:');
  
  // Change provider
  await browser_click('Settings button', '<ref>');
  await browser_select_option('Provider dropdown in settings', '<ref>', ['OpenAI']);
  await browser_click('Close settings button', '<ref>');
  
  // Send another message
  await browser_type('Chat input', '<ref>', 'What are the key points?');
  await browser_click('Send button', '<ref>');
  await browser_wait_for({ text: 'The key points are' });
});
```

---

### Flow 3: Analytics Monitoring 🟠 P1

**Steps:**
1. Navigate to analytics
2. Verify KPIs load
3. Check charts render
4. Drag widget to new position
5. Resize widget
6. Verify layout persists

**Playwright-MCP Implementation:**
```typescript
test('E2E: Analytics dashboard interaction', async () => {
  await browser_navigate('http://localhost:5173/analytics');
  await browser_wait_for({ text: 'Total Conversations' });
  
  // Verify KPIs
  const snapshot = await browser_snapshot();
  expect(snapshot).toContain('Total Conversations');
  expect(snapshot).toContain('Total Tokens');
  
  // Drag widget
  await browser_drag(
    'Usage Trends widget', '<start-ref>',
    'Cost Breakdown widget', '<end-ref>'
  );
  
  // Verify persistence (refresh page)
  await browser_navigate('http://localhost:5173/analytics');
  const newSnapshot = await browser_snapshot();
  // Layout should match after refresh
});
```

---

## Implementation Examples

### Test File Structure

```typescript
// webclient/src/test/e2e/landing-page.spec.ts
import { describe, test, expect, beforeAll, afterAll } from 'vitest';
import { 
  browser_navigate, 
  browser_snapshot, 
  browser_click,
  browser_wait_for,
  browser_close
} from '@playwright-mcp/tools'; // Hypothetical import

describe('Landing Page E2E Tests', () => {
  beforeAll(async () => {
    // Start dev server or use test environment
    await browser_navigate('http://localhost:5173/');
  });

  afterAll(async () => {
    await browser_close();
  });

  test('displays main heading and CTA', async () => {
    const snapshot = await browser_snapshot();
    expect(snapshot).toContain('AI Knowledge Manager');
    expect(snapshot).toContain('Manage Knowledge');
  });

  test('navigates to knowledge list on CTA click', async () => {
    await browser_click('Manage Knowledge button', '<ref>');
    await browser_wait_for({ text: 'Knowledge Bases' });
    const snapshot = await browser_snapshot();
    expect(snapshot).toContain('Knowledge Bases');
  });
});
```

---

### Accessibility Testing Example

```typescript
test('Chat page meets WCAG accessibility standards', async () => {
  await browser_navigate('http://localhost:5173/chat');
  const snapshot = await browser_snapshot();
  
  // Verify ARIA labels
  expect(snapshot).toContain('aria-label="Chat input"');
  expect(snapshot).toContain('aria-label="Send message"');
  expect(snapshot).toContain('aria-label="Select knowledge base"');
  
  // Test keyboard navigation
  await browser_press_key('Tab');
  await browser_press_key('Tab');
  await browser_press_key('Enter');
  
  // Verify focus management
  const focusedSnapshot = await browser_snapshot();
  expect(focusedSnapshot).toContain('focus'); // Check focused element
});
```

---

### Responsive Testing Example

```typescript
test('Chat page is responsive on mobile', async () => {
  await browser_navigate('http://localhost:5173/chat');
  
  // Test desktop
  await browser_resize(1920, 1080);
  const desktopSnapshot = await browser_take_screenshot();
  
  // Test tablet
  await browser_resize(768, 1024);
  const tabletSnapshot = await browser_take_screenshot();
  
  // Test mobile
  await browser_resize(375, 667);
  const mobileSnapshot = await browser_take_screenshot();
  
  // Verify layouts differ appropriately
  expect(desktopSnapshot).not.toEqual(mobileSnapshot);
});
```

---

### Network Monitoring Example

```typescript
test('Chat sends correct API request', async () => {
  await browser_navigate('http://localhost:5173/chat');
  
  // Send message
  await browser_type('Chat input', '<ref>', 'Test message');
  await browser_click('Send button', '<ref>');
  
  // Check network requests
  const requests = await browser_network_requests();
  const chatRequest = requests.find(req => req.url.includes('/api/chat'));
  
  expect(chatRequest).toBeDefined();
  expect(chatRequest.method).toBe('POST');
  expect(chatRequest.body).toContain('Test message');
});
```

---

## Test Organization & Structure

### Recommended Directory Structure

```
webclient/
├── src/
│   └── test/
│       ├── unit/                    # Vitest unit tests
│       │   ├── components/
│       │   │   ├── Button.test.tsx
│       │   │   └── ChatSettingsPanel.test.tsx
│       │   └── lib/
│       │       └── apiClient.test.ts
│       ├── integration/             # Integration tests
│       │   ├── chat-api.spec.ts
│       │   └── knowledge-api.spec.ts
│       ├── e2e/                     # Playwright-MCP E2E tests
│       │   ├── landing-page.spec.ts
│       │   ├── knowledge-list.spec.ts
│       │   ├── knowledge-form.spec.ts
│       │   ├── chat-page.spec.ts
│       │   ├── analytics-page.spec.ts
│       │   ├── models-page.spec.ts
│       │   └── workflows/           # Complete user flows
│       │       ├── upload-and-chat.spec.ts
│       │       ├── multi-provider-chat.spec.ts
│       │       └── analytics-monitoring.spec.ts
│       ├── accessibility/           # Accessibility-specific tests
│       │   ├── keyboard-navigation.spec.ts
│       │   ├── screen-reader.spec.ts
│       │   └── aria-compliance.spec.ts
│       ├── visual/                  # Visual regression tests
│       │   ├── landing-page.visual.spec.ts
│       │   └── chat-page.visual.spec.ts
│       ├── fixtures/                # Test data
│       │   ├── sample.pdf
│       │   ├── test-document.docx
│       │   └── mock-responses.json
│       ├── helpers/                 # Test utilities
│       │   ├── browser-helpers.ts
│       │   ├── api-helpers.ts
│       │   └── test-data.ts
│       ├── setup.ts                 # Global test setup
│       └── playwright-mcp.config.ts # Playwright-MCP config
└── playwright.config.ts             # Playwright configuration
```

---

## Best Practices

### 1. Test Data Management

**DO:**
- Use fixtures for consistent test data
- Clean up test data after tests
- Use unique identifiers (e.g., timestamps) for test resources
- Mock external API responses when possible

**DON'T:**
- Rely on production data
- Leave test data polluting the database
- Use hardcoded data that may change

```typescript
// helpers/test-data.ts
export function generateTestKnowledgeId(): string {
  return `test-kb-${Date.now()}`;
}

export async function cleanupTestKnowledge(id: string): Promise<void> {
  await fetch(`/api/knowledge/${id}`, { method: 'DELETE' });
}
```

---

### 2. Stability & Reliability

**DO:**
- Use explicit waits (`browser_wait_for`) instead of sleep
- Retry flaky tests with backoff
- Use accessibility snapshots for stable element selection
- Handle loading states explicitly

**DON'T:**
- Use fixed timeouts (e.g., `setTimeout(5000)`)
- Rely on element position (use semantic selectors)
- Ignore race conditions

```typescript
// Bad
await browser_click('Button', '<ref>');
await new Promise(resolve => setTimeout(resolve, 5000)); // ❌

// Good
await browser_click('Button', '<ref>');
await browser_wait_for({ text: 'Success' }); // ✅
```

---

### 3. Error Handling

**DO:**
- Capture screenshots on failure
- Log console errors
- Check network requests for errors
- Provide clear failure messages

**DON'T:**
- Suppress errors
- Use generic error messages
- Skip cleanup on failure

```typescript
test('Upload handles errors gracefully', async () => {
  try {
    await browser_navigate('http://localhost:5173/knowledge/upload');
    await browser_click('Upload without file button', '<ref>');
    
    // Expect error message
    await browser_wait_for({ text: 'Please select a file' });
    
    // Verify console has no errors
    const consoleMsgs = await browser_console_messages();
    const errors = consoleMsgs.filter(msg => msg.type === 'error');
    expect(errors.length).toBe(0);
  } catch (error) {
    // Capture screenshot for debugging
    await browser_take_screenshot({ filename: 'upload-error.png' });
    throw error;
  }
});
```

---

### 4. Test Independence

**DO:**
- Each test should be runnable in isolation
- Reset state between tests
- Use `beforeEach` / `afterEach` for setup/cleanup
- Avoid test interdependencies

**DON'T:**
- Rely on test execution order
- Share mutable state between tests
- Skip cleanup

```typescript
describe('Chat Page Tests', () => {
  let testKnowledgeId: string;

  beforeEach(async () => {
    // Setup fresh test data
    testKnowledgeId = generateTestKnowledgeId();
    await createTestKnowledge(testKnowledgeId);
    await browser_navigate(`http://localhost:5173/chat/${testKnowledgeId}`);
  });

  afterEach(async () => {
    // Cleanup
    await cleanupTestKnowledge(testKnowledgeId);
  });

  test('sends message', async () => {
    // Test is isolated
  });
});
```

---

### 5. Performance Considerations

**DO:**
- Run critical tests in parallel
- Use snapshots instead of screenshots when possible
- Cache common setup operations
- Monitor test execution time

**DON'T:**
- Run all tests serially
- Take unnecessary screenshots
- Navigate unnecessarily

```typescript
// playwright.config.ts
export default {
  workers: 4, // Run 4 tests in parallel
  timeout: 30000, // 30s timeout
  retries: 2, // Retry flaky tests
};
```

---

## Integration with CI/CD

### GitHub Actions Workflow Example

```yaml
# .github/workflows/ui-tests.yml
name: UI Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  playwright-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: 20
      
      - name: Install dependencies
        run: |
          cd webclient
          npm ci
      
      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium
      
      - name: Start backend (Docker)
        run: docker-compose up -d
      
      - name: Wait for services
        run: |
          timeout 60 bash -c 'until curl -f http://localhost:8080/api/ping; do sleep 2; done'
      
      - name: Run Playwright-MCP tests
        run: npm run test:e2e
        env:
          CI: true
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-results
          path: test-results/
      
      - name: Upload screenshots
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-screenshots
          path: screenshots/
```

---

## Maintenance & Monitoring

### 1. Test Health Monitoring

**Metrics to Track:**
- Test pass rate (target: 95%+)
- Test execution time (target: < 5 min for full suite)
- Flaky test rate (target: < 5%)
- Coverage percentage (target: 85%+)

**Tools:**
- Playwright Test Reporter
- GitHub Actions test summaries
- Custom dashboard (e.g., using Analytics page)

---

### 2. Regular Maintenance Tasks

**Weekly:**
- Review failing tests
- Update flaky tests
- Check test execution time

**Monthly:**
- Audit test coverage
- Remove obsolete tests
- Update test data fixtures
- Review accessibility compliance

**Quarterly:**
- Full accessibility audit
- Performance baseline tests
- Visual regression review
- Update Playwright/MCP versions

---

## Recommended Implementation Roadmap

### Phase 1: Foundation (Week 1-2)

**Goal:** Set up infrastructure and basic smoke tests

- [ ] Install Playwright and configure Playwright-MCP
- [ ] Create test directory structure
- [ ] Set up CI/CD pipeline
- [ ] Write 5 smoke tests (one per page)
- [ ] Create test fixtures and helpers

**Deliverables:**
- Playwright config file
- GitHub Actions workflow
- 5 passing smoke tests

**Estimated Effort:** 16 hours

---

### Phase 2: Critical Paths (Week 3-4)

**Goal:** Test P0 critical user flows

- [ ] Knowledge upload workflow (5 tests)
- [ ] Chat functionality (8 tests)
- [ ] Navigation between pages (3 tests)
- [ ] Provider selection (4 tests)

**Deliverables:**
- 20 passing E2E tests
- Test coverage report

**Estimated Effort:** 24 hours

---

### Phase 3: Integration & Error Handling (Week 5-6)

**Goal:** Test API integration and error scenarios

- [ ] API error handling (8 tests)
- [ ] Form validation (6 tests)
- [ ] Loading states (5 tests)
- [ ] Network failure scenarios (4 tests)

**Deliverables:**
- 23 passing integration tests
- Error scenario documentation

**Estimated Effort:** 20 hours

---

### Phase 4: Accessibility & Responsiveness (Week 7-8)

**Goal:** Ensure WCAG compliance and mobile support

- [ ] Keyboard navigation (6 tests)
- [ ] ARIA label compliance (8 tests)
- [ ] Mobile responsive tests (5 tests)
- [ ] Screen reader compatibility (4 tests)

**Deliverables:**
- 23 passing accessibility tests
- WCAG 2.1 compliance report

**Estimated Effort:** 20 hours

---

### Phase 5: Advanced Features (Week 9-10)

**Goal:** Test complex interactions

- [ ] Analytics drag-and-drop (5 tests)
- [ ] Ollama model management (7 tests)
- [ ] Real-time SignalR updates (4 tests)
- [ ] Dark mode toggle (3 tests)

**Deliverables:**
- 19 passing advanced feature tests
- Visual regression baselines

**Estimated Effort:** 18 hours

---

### Phase 6: Polish & Optimization (Week 11-12)

**Goal:** Stabilize and optimize test suite

- [ ] Fix flaky tests
- [ ] Optimize test execution time
- [ ] Add visual regression tests
- [ ] Document maintenance procedures
- [ ] Train team on Playwright-MCP

**Deliverables:**
- < 5% flaky test rate
- < 5 min test execution time
- Complete documentation

**Estimated Effort:** 16 hours

---

## Summary

### Expected Outcomes After Full Implementation:

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Test Coverage | ~0% | 85%+ | +85% |
| E2E Tests | 0 | 60+ | +60 |
| Critical Path Coverage | 0% | 100% | +100% |
| Accessibility Tests | 0 | 23+ | +23 |
| Visual Regression Tests | 0 | 10+ | +10 |
| CI/CD Integration | No | Yes | ✅ |

### Total Estimated Effort:
- **114 hours** (~3 months at 10 hours/week)
- **60+ new tests**
- **Complete CI/CD integration**
- **WCAG 2.1 compliance**

### ROI:
- **Reduced manual testing time:** 80% reduction
- **Faster bug detection:** Catch issues before production
- **Improved confidence:** Automated regression testing
- **Better accessibility:** WCAG compliance built-in

---

## Next Steps

1. **Review this document** with the development team
2. **Prioritize test scenarios** based on business impact
3. **Set up Playwright-MCP** in the project
4. **Start with Phase 1** (Foundation) ASAP
5. **Iterate incrementally** with regular reviews

---

**Document Version:** 1.0  
**Last Updated:** November 17, 2025  
**Maintained By:** Development Team
