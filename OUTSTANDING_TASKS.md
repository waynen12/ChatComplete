# Outstanding Tasks - ChatComplete AI Knowledge Manager

**Generated:** November 14, 2025  
**Status:** Comprehensive review of all instruction files  
**Priority Key:** 🔴 Critical | 🟠 High | 🟡 Medium | 🟢 Low

---

## Executive Summary

This document consolidates all outstanding tasks from:
1. `.github/copilot-instructions.md` - UI/UX development priorities
2. `UI_IMPROVEMENTS_ACTION_PLAN.md` - Detailed implementation guide
3. `CLAUDE.md` - Project milestones and technical context
4. `documentation/REMAINING_MILESTONES.md` - Backend roadmap

**Total Outstanding Tasks:** 100+ items across 4 major milestones

---

## Milestone #25: UI Modernization 🔄 IN PROGRESS (Copilot-Driven)

**Branch:** `copilot/review-ui-in-webclient`  
**Owner:** GitHub Copilot Cloud  
**Status:** Week 1 tasks pending  
**Estimated Effort:** 76 hours (4 weeks)

### Week 1: Critical Accessibility & Performance (16 hours) 🔴

#### Color Scheme Overhaul (DO FIRST - Highest Priority)
- [ ] Replace all blue-shaded colors with minimalist light palette
- [ ] Update primary color to `oklch(0.86 0.01 262.85)` (light lavender-gray)
- [ ] Darken button text for contrast with lighter backgrounds
- [ ] Update `webclient/src/styles/globals.css` CSS variables
- [ ] Update `webclient/tailwind.config.js` if needed
- [ ] Ensure WCAG 2.1 AA contrast ratios (4.5:1 for text, 3:1 for UI)
- [ ] Test all components in light mode
- [ ] Test all components in dark mode
- [ ] Verify color changes don't break UI states (hover, active, disabled)
- [ ] Run contrast checker on all text elements
- [ ] Check forms: input borders and states clear
- [ ] Verify buttons: text readable on new backgrounds

**Files to Modify:**
- `webclient/src/styles/globals.css`
- `webclient/tailwind.config.js`
- Any components with hardcoded blue colors

#### Accessibility Basics
- [ ] Add ARIA labels to all buttons (Settings button, Close button, etc.)
- [ ] Add ARIA labels to all links
- [ ] Add ARIA labels to all inputs
- [ ] Implement keyboard navigation for chat interface (Tab, Enter, Escape)
- [ ] Add skip navigation link to `src/layouts/AppLayout.tsx`
- [ ] Add CSS for `.sr-only` and `.focus:not-sr-only` classes
- [ ] Fix form label associations in `KnowledgeFormPage.tsx`
- [ ] Test with keyboard-only navigation
- [ ] Document keyboard testing approach

**Files to Modify:**
- `src/pages/ChatPage.tsx` (lines 234-240, 74-79)
- `src/components/ChatSettingsPanel.tsx` (lines 74-80)
- `src/layouts/AppLayout.tsx`
- `src/pages/KnowledgeFormPage.tsx`

#### Performance Optimization
- [ ] Implement route-based code splitting in `src/routes.tsx`
- [ ] Lazy load AnalyticsPage, ChatPage, KnowledgeListPage, KnowledgeFormPage
- [ ] Keep LandingPage and NotFoundPage in main bundle
- [ ] Add PageWrapper for Suspense boundaries
- [ ] Split Recharts into separate chunk in `vite.config.ts`
- [ ] Configure manual chunks for vendor, ui, recharts
- [ ] Add React.memo to expensive components
- [ ] Measure and document bundle size before/after
- [ ] Target: Reduce from 1.15 MB to < 800 KB

**Files to Modify:**
- `src/routes.tsx`
- `vite.config.ts`

#### Mobile Responsiveness
- [ ] Implement hamburger menu in `src/layouts/AppLayout.tsx`
- [ ] Add mobile menu state management
- [ ] Add Menu and X icons from lucide-react
- [ ] Desktop nav: hidden on mobile, visible on md+
- [ ] Mobile nav: full-width buttons, slide-down menu
- [ ] Ensure touch targets are 44px minimum
- [ ] Test on mobile devices or responsive mode

**Files to Modify:**
- `src/layouts/AppLayout.tsx`

**Week 1 Deliverable:** PR with new minimalist color scheme, 0 accessibility errors, < 800 KB bundle, working mobile nav

---

### Week 2: High Priority UX (24 hours) 🟠

#### Chat Experience Enhancements
- [ ] Add message actions component (copy, edit, delete buttons)
- [ ] Implement hover state for messages
- [ ] Add Copy icon button with clipboard functionality
- [ ] Add Edit icon button (user messages only)
- [ ] Add Delete icon button (user messages only)
- [ ] Implement `handleCopyMessage()` function
- [ ] Implement `handleEditMessage()` function
- [ ] Implement `handleDeleteMessage()` function
- [ ] Add toast notifications for actions
- [ ] Improve chat input: Shift+Enter for newlines
- [ ] Update textarea placeholder with Shift+Enter hint
- [ ] Add onKeyDown handler for Enter vs Shift+Enter
- [ ] Add conversation search/filter functionality
- [ ] Add file upload preview
- [ ] Implement chat input auto-resize

**Files to Modify:**
- `src/pages/ChatPage.tsx`

#### Form Improvements
- [ ] Add inline validation with helpful messages
- [ ] Add proper labels for collection name input
- [ ] Add proper labels for file upload input
- [ ] Add `aria-required` to required inputs
- [ ] Add `aria-describedby` for help text
- [ ] Improve error states with clear messages
- [ ] Add loading states to all async actions
- [ ] Add loading spinner to send button
- [ ] Add typing indicator while AI responds
- [ ] Implement form auto-save (where appropriate)

**Files to Modify:**
- `src/pages/KnowledgeFormPage.tsx`
- `src/pages/ChatPage.tsx`

#### Empty States
- [ ] Create reusable `<EmptyState />` component
- [ ] Add empty state to KnowledgeListPage with icon
- [ ] Add empty state with helpful message
- [ ] Add action button in empty state
- [ ] Add empty state to ChatPage
- [ ] Add empty state to AnalyticsPage
- [ ] Include search-specific empty states

**Files to Create:**
- `src/components/EmptyState.tsx`

**Files to Modify:**
- `src/pages/KnowledgeListPage.tsx`
- `src/pages/ChatPage.tsx`
- `src/pages/AnalyticsPage.tsx`

**Week 2 Deliverable:** PR with rich chat experience, improved forms, helpful empty states

---

### Week 3: Medium Priority Polish (20 hours) 🟡

#### Conversation Management
- [ ] Create `<ConversationHistory />` component
- [ ] Add conversation list sidebar (264px wide)
- [ ] Implement conversation selection
- [ ] Add conversation preview text
- [ ] Show message count per conversation
- [ ] Add conversation delete button with confirmation
- [ ] Implement conversation rename functionality
- [ ] Show conversation metadata (date, message count)
- [ ] Add conversation API integration (TODO: backend support needed)

**Files to Create:**
- `src/components/ConversationHistory.tsx`

**Files to Modify:**
- `src/pages/ChatPage.tsx`

#### Keyboard Shortcuts
- [ ] Create `useKeyboardShortcuts` hook
- [ ] Implement Ctrl+K to toggle settings panel
- [ ] Implement Ctrl+N for new conversation
- [ ] Add shortcuts modal (? key to show help)
- [ ] Create `<KeyboardShortcuts />` component
- [ ] Document shortcuts in UI
- [ ] Add global shortcut listeners

**Files to Create:**
- `src/hooks/useKeyboardShortcuts.ts`
- `src/components/KeyboardShortcuts.tsx`

**Files to Modify:**
- `src/pages/ChatPage.tsx`

#### Settings Improvements
- [ ] Reorganize settings into tabs
- [ ] Add settings search functionality
- [ ] Improve provider configuration UX
- [ ] Add better visual hierarchy

**Files to Modify:**
- Settings-related components

**Week 3 Deliverable:** PR with conversation management, keyboard shortcuts, improved settings

---

### Week 4: Testing & Polish (16 hours) 🟢

#### Cross-browser Testing
- [ ] Test all changes in Chrome
- [ ] Test all changes in Firefox
- [ ] Test all changes in Safari
- [ ] Fix any browser-specific issues
- [ ] Test mobile experience on real iOS device
- [ ] Test mobile experience on real Android device
- [ ] Document mobile testing results

#### Accessibility Audit
- [ ] Run axe-core DevTools on all pages
- [ ] Run Lighthouse accessibility audit (target: 90+)
- [ ] Manual keyboard navigation testing (document results)
- [ ] Screen reader testing with NVDA/JAWS/VoiceOver
- [ ] Test color contrast with WCAG checker
- [ ] Verify focus indicators are visible
- [ ] Test ARIA labels and live regions
- [ ] Fix any issues found

#### Performance Verification
- [ ] Bundle size analysis (before vs after)
- [ ] Lighthouse performance audit (target: 90+)
- [ ] Test First Contentful Paint (target: < 1.5s)
- [ ] Test Largest Contentful Paint (target: < 2.5s)
- [ ] Test Time to Interactive (target: < 3.5s)
- [ ] Test Total Blocking Time (target: < 300ms)
- [ ] Real-world performance testing
- [ ] Document performance improvements

#### Documentation
- [ ] Update UI_REVIEW.md with improvements made
- [ ] Document new components created
- [ ] Create user-facing changelog
- [ ] Update README with new features
- [ ] Add screenshots to documentation

**Week 4 Deliverable:** Polished, tested, production-ready UI improvements

---

### Additional UI Tasks (From Action Plan)

#### Virtual Scrolling (Optional)
- [ ] Install react-window package
- [ ] Add virtual scrolling for knowledge list when > 50 items
- [ ] Implement FixedSizeList component
- [ ] Test performance with large lists

**Files to Modify:**
- `src/pages/KnowledgeListPage.tsx`

#### Landing Page Enhancements
- [ ] Add multiple CTAs (Upload Documents + Start Chatting)
- [ ] Add feature highlights grid
- [ ] Add icons for features (Upload, Zap, BarChart3)
- [ ] Improve gradient background
- [ ] Make fully responsive

**Files to Modify:**
- `src/pages/LandingPage.tsx`

#### Responsive Design Fixes
- [ ] Fix ChatSettingsPanel width on mobile (100% vs 380px)
- [ ] Make analytics charts responsive (verify ResponsiveContainer)
- [ ] Test all responsive breakpoints (320px, 375px, 768px, 1024px, 1920px)

**Files to Modify:**
- `src/components/ChatSettingsPanel.tsx`
- Analytics components

---

## Milestone #23: MCP OAuth 2.1 Authentication ⚠️ BLOCKED

**Status:** BLOCKED on Auth0 JWE token issue  
**Priority:** High (for production deployment)  
**Estimated Effort:** 2-3 weeks (once unblocked)

### Completed ✅
- [x] JWT Bearer authentication with Auth0 integration
- [x] Scope-based authorization (mcp:read, mcp:execute, mcp:admin)
- [x] Token validation via JWKS endpoint
- [x] RFC 9728 OAuth metadata endpoint
- [x] WWW-Authenticate header for 401 responses
- [x] Configurable OAuth (enable/disable via appsettings.json)
- [x] Successfully tested with Postman and MCP Inspector
- [x] M2M Client Credentials Flow working

### Blocked ⚠️
- [ ] PKCE Authorization Code Flow (Auth0 returning JWE tokens)
- [ ] GitHub Copilot PKCE integration

### Investigation Needed
- [ ] Test with Azure AD as OAuth provider
- [ ] Test with AWS Cognito as OAuth provider
- [ ] Test with Keycloak as OAuth provider
- [ ] Determine if JWE encryption is Auth0-specific
- [ ] Research JWE decryption support if needed
- [ ] Document findings and recommended OAuth provider

**Blocker Details:**
- Auth0 returns encrypted JWE tokens: `{"alg":"dir","enc":"A256GCM"}`
- JWT Bearer middleware expects RS256 with kid (standard JWT)
- Cannot validate JWE tokens without decryption
- Blocks GitHub Copilot integration which uses PKCE

**Next Steps:**
1. Test alternative OAuth providers
2. Determine provider-specific behavior
3. Implement solution based on findings
4. Update documentation with recommended provider

---

## Milestone #24: MCP Client Development 🔄 IN PROGRESS

**Repository:** `/home/wayne/repos/McpClient` (separate repository)  
**Status:** Phase 1 (STDIO Transport) in progress  
**Priority:** Medium  
**Estimated Effort:** 6 weeks total

### Phase 1: STDIO Transport (Weeks 1-3) 🔄

#### Week 1: Transport Interface & STDIO Implementation (5 days)
- [x] Repository created and initialized
- [x] Solution and project setup (.NET 9)
- [x] NuGet packages installed
- [x] Basic STDIO connection working
- [x] Tool discovery functional
- [x] OpenAI integration (function calling)
- [ ] Create `ITransport` interface
- [ ] Implement `StdioTransport` class
- [ ] Add connection lifecycle management
- [ ] Add message serialization/deserialization
- [ ] Add error handling and retry logic
- [ ] Write unit tests for transport

**Deliverable:** Working STDIO transport connecting to Knowledge Manager

#### Week 2: Service Layer & CLI (5 days)
- [ ] Create `McpClientService` (main service)
- [ ] Create `DiscoveryService` (tools/resources)
- [ ] Create `ExecutionService` (tool execution)
- [ ] Implement tool discovery logic
- [ ] Implement resource discovery logic
- [ ] Implement tool execution logic
- [ ] Add configuration management
- [ ] Create CLI entry point with Spectre.Console
- [ ] Add interactive menu system
- [ ] Write unit tests for services

**Deliverable:** Interactive CLI for MCP client operations

#### Week 3: Testing & Documentation (5 days)
- [ ] Write integration tests (connect to Knowledge Manager)
- [ ] Test all 11 tools from Knowledge Manager
- [ ] Test all 6 resources from Knowledge Manager
- [ ] Test error scenarios (connection failures, timeouts)
- [ ] Performance testing
- [ ] Create user documentation
- [ ] Create API documentation
- [ ] Add code examples
- [ ] Create troubleshooting guide

**Deliverable:** Fully tested STDIO transport with documentation

### Phase 2: HTTP Transport (Weeks 4-6) 🛠️ TODO

#### Week 4: HTTP Transport Client-Side (5 days)
- [ ] Create `HttpSseTransport` class
- [ ] Implement HTTP connection logic
- [ ] Add Server-Sent Events (SSE) handling
- [ ] Implement session management
- [ ] Add MCP-Session-Id header handling
- [ ] Write unit tests for HTTP transport

**Deliverable:** Working HTTP transport

#### Week 5: SSE Handling & Reconnection (5 days)
- [ ] Implement SSE event parsing
- [ ] Add reconnection logic with exponential backoff
- [ ] Implement session persistence
- [ ] Add connection health checks
- [ ] Handle connection errors gracefully
- [ ] Write integration tests

**Deliverable:** Robust HTTP transport with reconnection

#### Week 6: Integration Tests & Polish (5 days)
- [ ] Cross-transport compatibility tests
- [ ] Test STDIO and HTTP transports side-by-side
- [ ] Performance comparison (STDIO vs HTTP)
- [ ] Load testing
- [ ] Update documentation
- [ ] Add HTTP-specific examples
- [ ] Create deployment guide

**Deliverable:** Production-ready MCP client

### Success Criteria

**Phase 1 Complete:**
- [ ] Clean architecture with ITransport interface
- [ ] StdioTransport fully functional
- [ ] Can connect to Knowledge Manager via STDIO
- [ ] Discovers all 11 tools correctly
- [ ] Executes tools successfully
- [ ] Discovers all 6 resources
- [ ] Reads resource content
- [ ] Interactive CLI with Spectre.Console
- [ ] Unit tests >80% coverage
- [ ] Integration tests passing

**Phase 2 Complete:**
- [ ] HttpSseTransport implemented
- [ ] SSE streaming working
- [ ] Reconnection logic robust
- [ ] Both transports configurable
- [ ] Performance acceptable (<100ms overhead)
- [ ] Cross-transport tests passing
- [ ] Documentation complete

---

## Milestone #12: Build+Deploy CI Refactor 🟢 DEFERRED

**Status:** Deferred (Low Priority)  
**Priority:** Low  
**Estimated Effort:** 1-2 days

### Planned Improvements
- [ ] Split monolithic CI/CD pipeline into build + deploy jobs
- [ ] Create separate build job (compile, test, package)
- [ ] Create separate deploy job (publish, restart service)
- [ ] Add artifact caching between jobs
- [ ] Implement matrix builds for multiple environments
- [ ] Add parallel test execution
- [ ] Update documentation

### Why Deferred
- Current CI/CD pipeline is stable and works correctly
- No performance bottlenecks
- No blocking issues
- Higher priority on MCP and UI work
- Can be addressed during slow period

---

## Backend Enhancements (Not Assigned to Copilot)

### Potential Future Milestones

#### Advanced Analytics (Milestone Candidate)
- [ ] Usage trend analysis
- [ ] Cost tracking per provider
- [ ] Token consumption forecasting
- [ ] Model performance comparisons
- [ ] Knowledge base quality metrics

#### Advanced RAG Features (Milestone Candidate)
- [ ] Hybrid search (semantic + keyword)
- [ ] Reranking models
- [ ] Query expansion
- [ ] Context optimization
- [ ] Citation tracking
- [ ] Multi-hop reasoning

#### Performance Optimization (Milestone Candidate)
- [ ] Redis caching layer
- [ ] Query result caching
- [ ] Embedding cache
- [ ] Connection pooling
- [ ] Batch processing
- [ ] Index optimization

#### Enterprise Features (Milestone Candidate)
- [ ] Multi-tenancy
- [ ] SSO integration (SAML, OAuth)
- [ ] Audit logging
- [ ] Compliance reports
- [ ] Data retention policies
- [ ] Backup/restore automation

---

## Priority Summary

### Immediate Focus (This Sprint)
1. **🔴 UI Week 1** - Color scheme, accessibility, performance, mobile (16 hours)
2. **🔴 MCP Client Phase 1** - Complete STDIO transport (15 days remaining)

### Next Sprint
1. **🟠 UI Week 2** - Chat UX, forms, empty states (24 hours)
2. **🟠 MCP Client Phase 2** - HTTP transport (15 days)

### Backlog
1. **🟡 UI Week 3** - Conversation management, shortcuts (20 hours)
2. **🟡 UI Week 4** - Testing and polish (16 hours)
3. **⚠️ OAuth 2.1** - Unblock PKCE flow (blocked on provider testing)
4. **🟢 CI/CD Refactor** - Split build/deploy jobs (deferred)

---

## Key Decisions & Constraints

### UI Development
- **Owner:** GitHub Copilot Cloud (autonomous)
- **Branch:** `copilot/review-ui-in-webclient`
- **Constraints:** No backend changes allowed
- **Communication:** PRs with screenshots and testing checklists
- **Review:** Human developer reviews and merges

### Backend Development
- **Owner:** Human developer
- **Constraints:** Minimal UI changes
- **Focus:** API stability, MCP server, OAuth, database

### MCP Client
- **Owner:** Human developer
- **Repository:** Separate (`/home/wayne/repos/McpClient`)
- **Coordination:** Version tagging with Knowledge Manager

---

## Success Metrics

### UI Modernization (Milestone #25)
- [ ] Bundle size: 1.15 MB → < 500 KB (57% reduction)
- [ ] Accessibility: 4/10 → 10/10 (WCAG 2.1 AA)
- [ ] Performance: 6/10 → 9/10 (Lighthouse 90+)
- [ ] Mobile: Incomplete → Full feature parity
- [ ] Color scheme: Blue-heavy → Minimalist light

### MCP Client (Milestone #24)
- [ ] STDIO transport: Functional
- [ ] HTTP transport: Functional
- [ ] Unit tests: >80% coverage
- [ ] Integration tests: All passing
- [ ] Documentation: Complete

### OAuth 2.1 (Milestone #23)
- [ ] PKCE flow: Unblocked and functional
- [ ] Alternative provider: Tested and documented
- [ ] GitHub Copilot: Successfully integrated

---

## Documentation Status

### Created
- ✅ `.github/copilot-instructions.md` (660 lines)
- ✅ `documentation/UI_REVIEW.md` (647 lines)
- ✅ `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` (1,015 lines)
- ✅ `documentation/REVIEW_SUMMARY.md` (225 lines)
- ✅ `documentation/REMAINING_MILESTONES.md` (503 lines)
- ✅ `CLAUDE.md` (1,200+ lines)

### Needs Updates (After Task Completion)
- [ ] `UI_REVIEW.md` - Update with improvements made
- [ ] `REMAINING_MILESTONES.md` - Mark tasks complete
- [ ] `CLAUDE.md` - Update milestone status
- [ ] User-facing changelog

---

## Questions or Blockers?

### Current Blockers
1. **OAuth PKCE** - Auth0 JWE token issue (needs provider testing)

### Clarifications Needed
1. MCP Client - Should HTTP transport wait for OAuth 2.1 completion?
2. UI Modernization - Should we merge incrementally or wait for all 4 weeks?

### Future Decisions
1. Which OAuth provider to recommend (Azure AD, AWS Cognito, Keycloak)?
2. Should MCP Client support OAuth from Phase 1?
3. Timeline for CI/CD refactor (deferred but when)?

---

## Next Steps

**Immediate Actions:**
1. GitHub Copilot Cloud starts UI Week 1 (color scheme overhaul)
2. Human developer continues MCP Client Phase 1 (service layer)
3. Research alternative OAuth providers for PKCE flow
4. Monitor UI PR submissions from Copilot branch

**This Week:**
- UI: Complete color scheme overhaul + accessibility basics
- MCP Client: Complete service layer + CLI
- OAuth: Test Azure AD as alternative provider

**Next Week:**
- UI: Start Week 2 (chat UX improvements)
- MCP Client: Complete Phase 1 testing
- OAuth: Document findings and recommendations

---

**Last Updated:** November 14, 2025  
**Next Review:** End of UI Week 1 / MCP Client Phase 1  
**Document Owner:** ChatComplete Project Team
