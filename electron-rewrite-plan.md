Goals

1.  LLM-friendly E2E testing - Playwright first-class Electron support, no hacks
2.  Smaller footprint - ~100MB vs current ~200MB
3.  Faster startup - <3s cold start
4.  Microsoft SSO support - Hard requirement
5.  All features - Tabs, workspaces, downloads, terminal, etc.

Current Pain Point

No viable E2E testing for CefSharp/WPF:

- Relies on hacked test application
- Custom JS injection to interact with browser content
- LLM automation attempts have completely failed
- No standard tooling works

Why Electron

- Playwright first-class support - drives entire app, no hacks
- Full Chromium control (downloads, keyboard, requests)
- Unified TypeScript codebase
- Standard tooling LLMs understand well

// E2E tests become straightforward
const app = await electron.launch({ args: ['main.js'] });
const window = await app.firstWindow();
await window.click('[data-testid="new-tab"]');

---

Phase 1: SSO Validation MVP

Goal: Minimal Electron app that loads a website - test if MS SSO works

Deliverables:

1.  Electron app with single BrowserWindow
2.  URL bar to navigate
3.  Test: navigate to MS-authenticated work site, verify login works

Why first: SSO is hard requirement. If Electron webview doesn't support MS SSO natively, need to investigate
alternatives (Azure AD OAuth, extension loading) before investing in full migration.

Tech stack:

- Electron + Vite
- React (minimal - just URL bar)
- TypeScript strict mode

---

Phase 2: Core Shell (after SSO validated)

- Webview-based tab management
- Basic window chrome (URL bar, tab strip)
- Keyboard shortcuts
- Playwright E2E test setup

Phase 3: Feature Migration

Port features one at a time with tests:

- Workspaces & tab persistence
- Downloads
- Find in page
- Zoom
- Domain/tab customization
- Terminal (xterm.js)

---

Architecture

renderer process (single window)
├── features/ # Feature modules
│ ├── tabs.ts # Manages <webview> elements
│ ├── downloads.ts
│ └── workspaces.ts
├── state/ # Zustand store, file persistence via preload
├── ui/ # React components
└── webview-tabs/ # Each tab = <webview> tag

main process (minimal)
├── window.ts # Creates BrowserWindow
├── native.ts # File system ops via preload
└── preload.ts # Bridge to main

---

Verification Plan

Phase 1 (MVP):

1.  npm run dev - starts Electron app
2.  Navigate to MS-authenticated work URL
3.  Verify SSO login completes successfully

After full migration:

1.  npm test - Vitest unit tests, <10s
2.  npm run e2e - Playwright E2E tests, <60s
3.  Cold start measured <3s

---

Decisions Made

- Framework: Electron + React + TypeScript
- Testing: Vitest (unit) + Playwright (E2E)
- Architecture: Unified TypeScript, features in renderer
- State: Zustand (can defer decision)

Remaining Questions

1.  SSO mechanism unknown - Will test in MVP if it "just works". If not, need to investigate Azure AD OAuth or
    extension loading.
