# E2E Tests

## Overview

This directory contains end-to-end (E2E) tests using Playwright for the ChatComplete web application.

## Running Tests

### Run all tests
```bash
npm run test:e2e
```

### Run in UI mode (interactive)
```bash
npm run test:e2e:ui
```

### Run in debug mode
```bash
npm run test:e2e:debug
```

### View test report
```bash
npm run test:e2e:report
```

## Test Structure

- `landing-page.spec.ts` - Tests for the landing/home page
- `knowledge-list.spec.ts` - Tests for the knowledge bases list page
- `knowledge-form.spec.ts` - Tests for the knowledge base creation/edit form
- `chat-page.spec.ts` - Tests for the chat interface
- `analytics-page.spec.ts` - Tests for the analytics dashboard

## Phase 1 Status

✅ Infrastructure setup complete:
- Playwright installed and configured
- Test directory structure created
- 5 smoke tests implemented (15 test cases total)
- Package.json scripts added

## Next Steps

See the Phase 2 roadmap in `documentation/PLAYWRIGHT_MCP_TESTING_REPORT.md` for:
- Critical path testing (Week 3-4)
- Form submissions and API integration
- User workflow testing
