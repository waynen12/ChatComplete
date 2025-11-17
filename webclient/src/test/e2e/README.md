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

### Run specific test file
```bash
npx playwright test chat-functionality
```

### View test report
```bash
npm run test:e2e:report
```

## Test Structure

### Phase 1 Tests (Smoke Tests)
- `landing-page.spec.ts` - Tests for the landing/home page (3 tests)
- `knowledge-list.spec.ts` - Tests for the knowledge bases list page (3 tests)
- `knowledge-form.spec.ts` - Tests for the knowledge base creation/edit form (3 tests)
- `chat-page.spec.ts` - Tests for the chat interface (3 tests)
- `analytics-page.spec.ts` - Tests for the analytics dashboard (3 tests)

### Phase 2 Tests (Critical Paths)
- `knowledge-upload-workflow.spec.ts` - Complete upload workflow (5 tests)
- `chat-functionality.spec.ts` - Chat features and provider selection (12 tests)
- `navigation.spec.ts` - Navigation between pages (8 tests)

**Total: 40 tests across 8 files**

## Phase 1 Status ✅

Infrastructure setup complete:
- Playwright installed and configured
- Test directory structure created
- 5 smoke test files (15 test cases total)
- Package.json scripts added

## Phase 2 Status ✅

Critical path testing complete:
- Knowledge upload workflow tests (5 tests)
- Chat functionality tests (12 tests)
- Navigation tests (8 tests)
- **Total added: 25 new tests**
- **Coverage increased to ~30%**

## Test Coverage Summary

| Phase | Tests | Coverage | Status |
|-------|-------|----------|--------|
| Phase 1: Foundation | 15 | ~10% | ✅ Complete |
| Phase 2: Critical Paths | 25 | +20% | ✅ Complete |
| **Current Total** | **40** | **~30%** | ✅ |
| Phase 3: Integration | TBD | +25% | 🔜 Next |
| Target (Phase 6) | 100+ | 85%+ | 🎯 Goal |

## Next Steps: Phase 3 - Integration & Error Handling

**Timeline:** Week 5-6 (20 hours)

**Planned Tests:**
1. **API Error Handling** (8 tests)
   - Network failures
   - Invalid responses
   - Timeout scenarios

2. **Form Validation** (6 tests)
   - Required fields
   - Input validation
   - Error messages

3. **Loading States** (5 tests)
   - Async operations
   - Progress indicators
   - Skeleton screens

**Reference:** See `documentation/PLAYWRIGHT_MCP_TESTING_REPORT.md` for detailed Phase 3 plan
