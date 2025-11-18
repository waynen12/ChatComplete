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

### Phase 3 Tests (Integration & Error Handling)
- `api-error-handling.spec.ts` - API error scenarios (8 tests)
- `form-validation.spec.ts` - Form validation rules (9 tests)
- `loading-states.spec.ts` - Loading indicators and async states (6 tests)
- `network-failures.spec.ts` - Network failure scenarios (8 tests)

**Total: 71 tests across 12 files**

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

## Phase 3 Status ✅

Integration & error handling complete:
- API error handling tests (8 tests)
- Form validation tests (9 tests)
- Loading states tests (6 tests)
- Network failure tests (8 tests)
- **Total added: 31 new tests**
- **Coverage increased to ~55%**

## Test Coverage Summary

| Phase | Tests | Coverage | Status |
|-------|-------|----------|--------|
| Phase 1: Foundation | 15 | ~10% | ✅ Complete |
| Phase 2: Critical Paths | 25 | +20% | ✅ Complete |
| Phase 3: Integration | 31 | +25% | ✅ Complete |
| **Current Total** | **71** | **~55%** | ✅ |
| Phase 4: Accessibility | TBD | +15% | 🔜 Next |
| Target (Phase 6) | 100+ | 85%+ | 🎯 Goal |

## Next Steps: Phase 4 - Accessibility & Responsiveness

**Timeline:** Week 7-8 (20 hours)

**Planned Tests:**
1. **Keyboard Navigation** (6 tests)
   - Tab navigation flow
   - Enter key actions
   - Escape key handlers

2. **ARIA Compliance** (8 tests)
   - Button labels
   - Form labels
   - Dialog roles

3. **Mobile Responsive** (5 tests)
   - Mobile viewport layouts
   - Touch interactions
   - Responsive breakpoints

**Reference:** See `documentation/PLAYWRIGHT_MCP_TESTING_REPORT.md` for detailed Phase 4 plan
