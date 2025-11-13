# GitHub Copilot Instructions for ChatComplete

## Project Overview

**ChatComplete** is an AI Knowledge Manager - an open-source full-stack application for uploading technical documentation, vector-indexing it, and chatting with multiple LLM providers over that knowledge.

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET 8 Minimal APIs, Serilog, Qdrant/MongoDB Atlas (vector store), Semantic Kernel 1.6 |
| **Frontend** | React 19, Vite, TypeScript, shadcn/ui (Radix + Tailwind), Framer Motion, SignalR |
| **Database** | SQLite (local config/chat history), Qdrant (vector embeddings) |
| **AI Providers** | OpenAI, Google Gemini, Anthropic Claude, Ollama (local models) |
| **Deployment** | Docker, Self-hosted GitHub Actions (Mint Linux) |

### Project Architecture

```
ChatComplete/
├── webclient/              # React frontend (YOUR PRIMARY FOCUS)
│   ├── src/
│   │   ├── components/     # Reusable UI components
│   │   ├── pages/          # Page components (Chat, Analytics, Models, Settings)
│   │   ├── hooks/          # Custom React hooks
│   │   ├── lib/            # Utilities, API client, types
│   │   └── styles/         # Tailwind CSS, OKLCH color system
│   ├── public/             # Static assets
│   └── vite.config.ts      # Vite configuration
├── Knowledge.Api/          # ASP.NET backend API
├── KnowledgeEngine/        # Core business logic
├── Knowledge.Mcp/          # MCP (Model Context Protocol) server
├── Knowledge.Analytics/    # Analytics services
└── documentation/          # Project documentation
```

---

## 🎯 Your Primary Focus: UI/UX Development

As GitHub Copilot Cloud, your primary responsibility is **UI/UX development** in the `webclient/` directory. You should focus exclusively on frontend improvements while the human developer handles backend changes.

### UI Development Guidelines

#### 1. **Code Quality Standards**

- ✅ **TypeScript**: Use proper type definitions, avoid `any` types
- ✅ **ESLint**: Maintain 0 errors (current status: 0/0 ✅)
- ✅ **React Best Practices**:
  - Use proper dependency arrays in hooks
  - Clean up effects and event listeners
  - Avoid prop drilling (use context when appropriate)
- ✅ **Accessibility**: Add ARIA labels, keyboard navigation, semantic HTML
- ✅ **Performance**: Code splitting, lazy loading, memoization

#### 2. **Design System**

**⚠️ Color System Update Required (Week 1 Priority):**

The current UI uses blue-shaded colors extensively. **Your first task is to replace all blue colors with a minimalist light palette.**

**NEW Color System (OKLCH):**
```css
/* NEW Primary brand colors (minimalist light theme) */
--primary: 0.86 0.01 262.85;           /* Light lavender-gray - MINIMALIST */
--primary-foreground: [TBD];           /* Darker text for contrast */

/* Semantic colors (keep these unless they're blue) */
--destructive: 0 84.2% 60.2%;          /* Red for delete actions */
--muted: 217.2 10% 25%;                /* Muted backgrounds */
--accent: 217.2 27.8% 32.5%;           /* Accent highlights */
```

**OLD Color System (DEPRECATED - Replace these):**
```css
/* OLD Primary colors (blue-shaded - REMOVE) */
--primary: 272.44 0.114 293.39;        /* Purple - DEPRECATED */
--primary-foreground: 284.21 0.084 300.12; /* DEPRECATED */
```

**Implementation Notes:**
- Replace ALL instances of blue colors in components
- Ensure 4.5:1 contrast ratio for text (WCAG 2.1 AA)
- Darken button text to maintain readability on lighter backgrounds
- Test thoroughly in both light and dark modes
- Update `globals.css` and `tailwind.config.js`
- Check for hardcoded blue values in component files

**Component Library:**
- Use **shadcn/ui** components (already installed)
- Follow **Radix UI** patterns for accessibility
- Extend with custom components in `src/components/`

**Layout Patterns:**
- Responsive: Mobile-first design (breakpoints: sm, md, lg, xl)
- Dark mode: Full support via `next-themes` (don't break this!)
- Spacing: Tailwind spacing scale (4px base unit)

#### 3. **Current UI State (Baseline)**

**✅ Strengths:**
- Modern React 19 + Vite + TypeScript setup
- Excellent OKLCH color system with full dark mode
- Real-time updates via SignalR
- Good component architecture
- Smooth animations with Framer Motion

**⚠️ Areas Needing Improvement (YOUR FOCUS):**

| Priority | Area | Issues | Target |
|----------|------|--------|--------|
| 🔴 **Critical** | **Color Scheme** | Blue-shaded colors throughout UI | Minimalist light palette |
| 🔴 **Critical** | **Accessibility** | Missing ARIA labels, keyboard nav | WCAG 2.1 AA |
| 🔴 **Critical** | **Performance** | 1.15 MB bundle size | < 500 KB |
| 🔴 **Critical** | **Mobile UX** | No hamburger menu, responsive issues | Full mobile support |
| 🟠 **High** | **Chat UX** | No message actions (copy/edit/delete) | Rich chat experience |
| 🟠 **High** | **Forms** | Missing labels, poor validation UX | Accessible forms |
| 🟡 **Medium** | **Empty States** | Generic "no data" messages | Helpful empty states |
| 🟡 **Medium** | **Keyboard Shortcuts** | Limited keyboard support | Power user shortcuts |

#### 4. **Required Reading Before Making Changes**

**MUST READ (in order):**
1. `documentation/REVIEW_SUMMARY.md` - Quick overview of current UI state
2. `documentation/UI_REVIEW.md` - Detailed UI/UX analysis (18 KB)
3. `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` - Implementation guide (26 KB)
4. `CLAUDE.md` - Project milestones and technical context

**Key Sections to Focus On:**
- UI_IMPROVEMENTS_ACTION_PLAN.md → Week 1-4 implementation timeline
- UI_REVIEW.md → Accessibility findings (Section 4)
- UI_REVIEW.md → Performance recommendations (Section 5)

---

## 🚫 What NOT to Do

### Backend Changes - DO NOT TOUCH

You should **NOT** make changes to:
- ❌ `Knowledge.Api/` - Backend API (C#)
- ❌ `KnowledgeEngine/` - Core business logic (C#)
- ❌ `Knowledge.Mcp/` - MCP server (C#)
- ❌ `Knowledge.Analytics/` - Analytics services (C#)
- ❌ `.github/workflows/` - CI/CD pipelines
- ❌ `docker-compose*.yml` - Docker configurations
- ❌ Database schemas or migrations
- ❌ API endpoints or contracts

**If a task requires backend changes:**
1. ⚠️ **STOP** - Do not implement backend code
2. 📝 Document the required backend change in your PR description
3. 🏷️ Tag the PR with `needs-backend-support` label
4. 💬 Explain what API changes are needed and why

### Configuration Files - Limited Changes

**Can modify (with caution):**
- ✅ `webclient/vite.config.ts` - Frontend build config (verify it doesn't break backend proxy)
- ✅ `webclient/package.json` - Add frontend dependencies only
- ✅ `webclient/tsconfig.json` - TypeScript config for frontend
- ✅ `webclient/tailwind.config.js` - Design tokens, theme extensions

**Cannot modify:**
- ❌ Root `package.json` (if exists)
- ❌ `.csproj` files (C# projects)
- ❌ `appsettings.json` files (backend config)

---

## ✅ What You SHOULD Do

### Primary Responsibilities

#### 1. **UI/UX Improvements**

**Accessibility (🔴 Critical Priority):**
- Add ARIA labels to all interactive elements
- Implement keyboard navigation (Tab, Enter, Escape, Arrow keys)
- Add skip navigation links
- Ensure form labels are properly associated
- Test with screen readers (document testing approach)

**Performance Optimization (🔴 Critical Priority):**
- Implement code splitting (lazy load pages)
- Optimize bundle size (target: < 500 KB)
- Add React.memo for expensive components
- Implement virtual scrolling for long lists
- Optimize images and assets

**Mobile Responsiveness (🔴 Critical Priority):**
- Implement hamburger menu for mobile navigation
- Fix responsive layout issues on small screens
- Ensure touch targets are at least 44x44px
- Test on real mobile devices (document testing)

**Chat Experience Enhancements (🟠 High Priority):**
- Add message actions (copy, edit, delete, regenerate)
- Implement rich text editor for chat input
- Add file upload preview and progress
- Improve conversation management UI
- Add conversation search/filter

**Form Improvements (🟠 High Priority):**
- Add proper labels and ARIA attributes
- Implement inline validation with helpful messages
- Add loading states and error recovery
- Improve settings page UX

#### 2. **Component Development**

**New Components to Create:**
- `<MessageActions />` - Copy/edit/delete message buttons
- `<ConversationList />` - Searchable conversation sidebar
- `<EmptyState />` - Reusable empty state component
- `<MobileMenu />` - Hamburger navigation for mobile
- `<SkipNav />` - Skip to main content link
- `<KeyboardShortcuts />` - Shortcuts modal/help

**Components to Refactor:**
- `ChatInput` - Add rich text editing, file previews
- `ChatMessage` - Add action buttons, better formatting
- `Sidebar` - Mobile responsive with hamburger toggle
- `OllamaModelManager` - Improve model download UX

#### 3. **Testing Requirements**

**Must test before submitting PR:**
- ✅ Desktop: Chrome, Firefox, Safari
- ✅ Mobile: iOS Safari, Android Chrome (or responsive mode)
- ✅ Dark mode: All changes work in both themes
- ✅ Accessibility: Keyboard navigation, screen reader test
- ✅ Performance: Bundle size check (`npm run build`)
- ✅ Build: `npm run build` succeeds without errors

**Document in PR:**
```markdown
## Testing Checklist
- [ ] Desktop browsers (Chrome, Firefox, Safari)
- [ ] Mobile responsive (tested at 375px, 768px, 1024px)
- [ ] Dark mode compatibility
- [ ] Keyboard navigation (Tab, Enter, Escape)
- [ ] Screen reader test (describe approach)
- [ ] Bundle size check (before: X MB, after: Y MB)
- [ ] Build succeeds without errors
```

#### 4. **Pull Request Standards**

**Branch Naming:**
```
copilot/[feature-name]

Examples:
copilot/add-message-actions
copilot/improve-mobile-navigation
copilot/accessibility-aria-labels
```

**Commit Message Format:**
```
[UI] Brief description of change

- Detailed point 1
- Detailed point 2
- Detailed point 3

Resolves: #issue-number (if applicable)
```

**PR Template:**
```markdown
## Changes
Brief description of what was changed and why.

## UI/UX Impact
- Before: [Screenshot or description]
- After: [Screenshot or description]

## Testing Checklist
- [ ] Desktop browsers
- [ ] Mobile responsive
- [ ] Dark mode
- [ ] Keyboard navigation
- [ ] Build succeeds

## Backend Dependencies
- [ ] None (pure frontend change)
- [ ] Requires backend support (describe below)

## Screenshots
[Add screenshots showing before/after, or key features]

## Notes for Reviewer
Any additional context or areas to focus on during review.
```

---

## 🎨 UI Implementation Priorities

### Week 1: Critical Accessibility & Performance (16 hours)

**Color Scheme Overhaul (🔴 Critical - Do First):**
1. Replace all blue-shaded colors with minimalist light palette
2. Update primary color to `oklch(0.86 0.01 262.85)` (light lavender-gray)
3. Darken button text for contrast with lighter backgrounds
4. Update `globals.css` CSS variables for light/dark themes
5. Update `tailwind.config.js` if needed
6. Ensure WCAG 2.1 AA contrast ratios (4.5:1 for text, 3:1 for UI elements)
7. Test all components in both light and dark modes
8. Verify color changes don't break existing UI states (hover, active, disabled)

**Files to modify:**
- `webclient/src/styles/globals.css` - Primary location for color variables
- `webclient/tailwind.config.js` - May need updates if custom colors defined
- Any components with hardcoded blue colors

**Target palette:**
- Primary: `oklch(0.86 0.01 262.85)` - Light lavender-gray
- Primary foreground: Darker text (TBD based on contrast testing)
- Keep existing destructive, muted, accent colors (unless they're blue)
- Maintain OKLCH color space for consistency

**Testing checklist:**
- [ ] Light mode: All colors visible and accessible
- [ ] Dark mode: All colors maintain contrast
- [ ] Buttons: Text readable on new background
- [ ] Forms: Input borders and states clear
- [ ] Hover/active states: Distinguishable
- [ ] Contrast checker: All text meets 4.5:1 minimum

**Deliverable:** PR with new minimalist color scheme, passing contrast checks

---

**Accessibility Basics:**
1. Add ARIA labels to all buttons, links, inputs
2. Implement keyboard navigation for chat interface
3. Add skip navigation link
4. Fix form label associations
5. Test with keyboard-only navigation

**Performance:**
1. Implement lazy loading for pages (React.lazy)
2. Add code splitting for heavy components
3. Optimize bundle size (target: reduce by 50%)
4. Add React.memo to expensive renders
5. Measure and document improvements

**Mobile:**
1. Implement hamburger menu for sidebar
2. Fix responsive breakpoints
3. Ensure touch targets are 44px minimum
4. Test on real mobile devices

**Deliverable:** PR with new colors, 0 accessibility errors, < 800 KB bundle, working mobile nav

### Week 2: High Priority UX (24 hours)

**Chat Enhancements:**
1. Add message actions (copy, edit, delete, regenerate)
2. Implement message hover states with actions
3. Add conversation search/filter
4. Improve chat input with auto-resize
5. Add file upload preview

**Form Improvements:**
1. Add inline validation with helpful messages
2. Improve error states and recovery
3. Add loading states to all async actions
4. Implement form auto-save (where appropriate)

**Empty States:**
1. Create reusable EmptyState component
2. Add helpful empty states to all pages
3. Include action buttons in empty states

**Deliverable:** PR with rich chat experience and improved forms

### Week 3: Medium Priority Polish (20 hours)

**Conversation Management:**
1. Add conversation list sidebar
2. Implement conversation rename
3. Add conversation delete with confirmation
4. Show conversation metadata (date, message count)

**Keyboard Shortcuts:**
1. Implement global shortcuts (Cmd+K for search, etc.)
2. Add shortcuts modal (? key to show help)
3. Document shortcuts in UI

**Settings Improvements:**
1. Reorganize settings into tabs
2. Add settings search
3. Improve provider configuration UX

**Deliverable:** PR with conversation management and shortcuts

### Week 4: Testing & Polish (16 hours)

**Cross-browser Testing:**
1. Test all changes in Chrome, Firefox, Safari
2. Fix any browser-specific issues
3. Verify mobile experience on real devices

**Accessibility Audit:**
1. Run automated accessibility tests
2. Manual keyboard navigation testing
3. Screen reader testing (document approach)
4. Fix any issues found

**Performance Verification:**
1. Bundle size analysis
2. Lighthouse audit (target: 90+ score)
3. Real-world performance testing

**Documentation:**
1. Update UI_REVIEW.md with improvements
2. Document new components
3. Create user-facing changelog

**Deliverable:** Polished, tested, production-ready UI improvements

---

## 📚 Reference Documentation

### Essential Files

**Project Documentation:**
- `CLAUDE.md` - Project overview, milestones, tech stack
- `README.md` - Setup instructions, deployment guide
- `documentation/REVIEW_SUMMARY.md` - Current UI state summary
- `documentation/UI_REVIEW.md` - Detailed UI analysis
- `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` - Implementation guide

**Frontend Code:**
- `webclient/src/lib/types.ts` - TypeScript interfaces
- `webclient/src/lib/apiClient.ts` - API client (don't change contracts)
- `webclient/src/components/` - Existing components to reference
- `webclient/src/pages/` - Page components to improve

**Design System:**
- `webclient/src/styles/globals.css` - CSS variables (OKLCH colors)
- `webclient/tailwind.config.js` - Tailwind configuration
- `webclient/src/components/ui/` - shadcn/ui components

### API Contracts (Read-Only)

**Key Endpoints:**
- `GET /api/knowledge` - List knowledge bases
- `POST /api/knowledge` - Upload documents
- `POST /api/chat` - Send chat message (SSE stream)
- `GET /api/analytics/providers` - Provider analytics
- `GET /api/models` - List Ollama models

**Important:** These contracts are owned by the backend. If you need changes, document them in your PR and tag with `needs-backend-support`.

### Color Palette Reference

**OKLCH Colors (copy these values):**
```css
/* Light mode */
--background: 0 0% 100%;
--foreground: 222.2 84% 4.9%;
--primary: 272.44 0.114 293.39;
--primary-foreground: 284.21 0.084 300.12;
--destructive: 0 84.2% 60.2%;

/* Dark mode */
--background: 222.2 84% 4.9%;
--foreground: 210 40% 98%;
--primary: 272.44 0.114 293.39;
--primary-foreground: 284.21 0.084 300.12;
```

Always use these CSS variables rather than hardcoding colors.

---

## 🤝 Collaboration Guidelines

### Communication

**When working on a task:**
1. 📝 Create a tracking comment in the issue/PR
2. 🎯 State your understanding of the task
3. 📋 List the files you plan to modify
4. ⏱️ Estimate completion time
5. 🚀 Proceed with implementation

**If you encounter blockers:**
1. 🛑 Stop work immediately
2. 📝 Document the blocker clearly
3. 🏷️ Tag the issue appropriately
4. 💬 Explain what backend support is needed (if applicable)

### Code Review Expectations

**Before requesting review:**
- ✅ All tests pass
- ✅ Build succeeds
- ✅ ESLint shows 0 errors
- ✅ Screenshots added to PR
- ✅ Testing checklist completed

**Review focus areas:**
- UI/UX improvements
- Accessibility compliance
- Performance impact
- Code quality and maintainability
- Dark mode compatibility

---

## 🔍 Common Pitfalls to Avoid

### 1. **Breaking Dark Mode**
❌ **Don't:** Use hardcoded colors like `bg-white` or `text-black`
✅ **Do:** Use CSS variables like `bg-background` and `text-foreground`

### 2. **Ignoring Accessibility**
❌ **Don't:** Use `<div onClick>` without keyboard support
✅ **Do:** Use `<button>` with proper ARIA labels

### 3. **Large Bundle Size**
❌ **Don't:** Import entire libraries like `import _ from 'lodash'`
✅ **Do:** Import specific functions like `import { debounce } from 'lodash-es'`

### 4. **Prop Drilling**
❌ **Don't:** Pass props through 5+ component levels
✅ **Do:** Use React Context or state management for deeply nested data

### 5. **Missing Loading States**
❌ **Don't:** Show blank screens during async operations
✅ **Do:** Show skeletons, spinners, or progress indicators

### 6. **Poor Error Handling**
❌ **Don't:** Let errors crash the UI silently
✅ **Do:** Use Error Boundaries and show helpful error messages

### 7. **Inconsistent Styling**
❌ **Don't:** Mix inline styles, CSS modules, and Tailwind randomly
✅ **Do:** Use Tailwind consistently with shadcn/ui components

---

## 📊 Success Metrics

### Code Quality Targets
- ✅ ESLint errors: **0** (currently: 0 ✅)
- ✅ TypeScript strict mode: **enabled**
- ✅ Bundle size: **< 500 KB** (currently: 1.15 MB ❌)
- ✅ Lighthouse score: **90+** (Performance, Accessibility, Best Practices)

### Accessibility Targets
- ✅ WCAG 2.1 Level AA compliance
- ✅ Keyboard navigation: 100% of features accessible
- ✅ Screen reader compatible: All interactive elements announced
- ✅ Color contrast: All text meets 4.5:1 minimum

### Performance Targets
- ✅ First Contentful Paint: < 1.5s
- ✅ Time to Interactive: < 3s
- ✅ Bundle size: < 500 KB gzipped
- ✅ Code splitting: All pages lazy loaded

### User Experience Targets
- ✅ Mobile responsive: Full feature parity on mobile
- ✅ Dark mode: Perfect in both themes
- ✅ Empty states: Helpful guidance on all pages
- ✅ Error recovery: Clear actions on all errors

---

## 🎓 Learning Resources

### React & TypeScript
- [React 19 Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [React TypeScript Cheatsheet](https://react-typescript-cheatsheet.netlify.app/)

### Accessibility
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

### Performance
- [Web.dev Performance](https://web.dev/performance/)
- [React Performance Optimization](https://react.dev/learn/render-and-commit#optimizing-performance)
- [Vite Build Optimization](https://vitejs.dev/guide/build.html#build-optimizations)

### Design System
- [shadcn/ui Documentation](https://ui.shadcn.com/)
- [Radix UI Components](https://www.radix-ui.com/)
- [Tailwind CSS](https://tailwindcss.com/)
- [OKLCH Color Picker](https://oklch.com/)

---

## 🚀 Getting Started Checklist

Before making your first change:

- [ ] Read `documentation/REVIEW_SUMMARY.md`
- [ ] Read `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` (Week 1 section)
- [ ] Review `CLAUDE.md` for project context
- [ ] Explore `webclient/src/` directory structure
- [ ] Run `npm run dev` and test the application
- [ ] Review existing components in `src/components/`
- [ ] Check current build size: `npm run build`
- [ ] Understand the OKLCH color system in `globals.css`
- [ ] Test dark mode toggle
- [ ] Create your first branch: `copilot/[your-first-task]`

**Your First Task (Recommended):**
Start with a small, high-impact change from Week 1:
- Add ARIA labels to chat interface buttons
- Implement keyboard navigation for message list
- Add skip navigation link

This will help you understand the codebase while making a meaningful accessibility improvement.

---

## 📞 Questions?

If you're unsure about:
- **UI/UX decisions**: Refer to `UI_IMPROVEMENTS_ACTION_PLAN.md`
- **Technical approach**: Check `UI_REVIEW.md` recommendations
- **Project context**: Read `CLAUDE.md` and `README.md`
- **Accessibility**: Consult WCAG 2.1 guidelines
- **Backend integration**: Document in PR, tag `needs-backend-support`

**Remember:** Your focus is UI/UX excellence. If a task requires backend changes, document it clearly and let the human developer handle the backend work.

---

## 🎯 Summary: Your Mission

**Primary Goal:** Transform the ChatComplete UI from a 7/10 to a 10/10 user experience.

**Focus Areas:**
1. 🔴 **Accessibility** - Make it usable for everyone
2. 🔴 **Performance** - Make it fast and efficient
3. 🔴 **Mobile** - Make it work beautifully on all devices
4. 🟠 **UX Polish** - Make it delightful to use

**Constraints:**
- ❌ No backend changes
- ✅ Pure frontend improvements
- ✅ Follow existing design system
- ✅ Maintain dark mode support
- ✅ Test thoroughly before PRs

**Success = Happy Users + Maintainable Code + Accessible Experience**

Let's build something amazing! 🚀
