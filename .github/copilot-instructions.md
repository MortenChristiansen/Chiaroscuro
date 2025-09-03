# Chiaroscuro Browser

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

Chiaroscuro is a Windows-only web browser built with C# WPF, CefSharp, and Angular. The C# application hosts the browser engine while Angular provides the UI chrome (tabs, address bar, etc.).

## Working Effectively

### Bootstrap, Build, and Test the Repository

**CRITICAL**: The .NET application targets Windows and WILL FAIL to build on Linux/macOS. This is expected and documented below.

1. **Install Dependencies** (if needed):
   - .NET 9.0+ SDK
   - Node.js 22+ and npm

2. **Build Angular Frontend**:
   ```bash
   cd src/chrome-app
   npm install  # Takes ~1-5 seconds if node_modules exists
   npm run build  # Takes ~20 seconds. NEVER CANCEL. Set timeout to 60+ minutes for safety.
   ```
   - Expected: Build succeeds with SSR prerender error (non-blocking)
   - Output location: `src/chrome-app/dist/chrome-app/browser/`

3. **Copy Angular Output to .NET Project**:
   ```bash
   cd src/BrowserHost
   rm -rf chrome-app
   cp -r ../chrome-app/dist/chrome-app/browser chrome-app
   ```

4. **Build .NET Application** (Windows only):
   ```bash
   cd src/BrowserHost
   dotnet build  # Windows only - FAILS on Linux/macOS as expected
   ```
   - On Linux/macOS: Shows error "NETSDK1100: To build a project targeting Windows" - this is EXPECTED
   - On Windows: Takes ~10-30 seconds. NEVER CANCEL. Set timeout to 60+ minutes for safety.

5. **Run Tests**:
   ```bash
   cd src/chrome-app
   npm test -- --watch=false --browsers=ChromeHeadless  # Takes ~20-30 seconds
   ```
   - Expected: 26+ tests run, may have 1 failing test (non-critical)
   - No .NET unit tests exist in this repository

### Development Workflow

**Frontend Development (Angular)**:
```bash
cd src/chrome-app
npx ng serve --host 0.0.0.0 --port 4200  # Takes ~10 seconds to start
```
- Access at: `http://localhost:4200`
- Use for UI development and testing Angular components

**Full Application** (Windows only):
- Build both Angular and .NET components
- Run the compiled .exe on Windows
- Cannot run the full browser on Linux/macOS

## Validation

**CRITICAL**: Always manually validate changes by running through complete scenarios:

1. **Angular Development Validation**:
   - Start dev server: `cd src/chrome-app && npx ng serve`
   - Verify UI loads and components render correctly
   - Test keyboard shortcuts and interactions in browser

2. **Build Validation**:
   - Always run the complete build sequence after changes
   - Verify Angular build completes without blocking errors
   - On Windows: Verify .NET build succeeds

3. **Test Validation**:
   - Run Angular tests: `npm test -- --watch=false --browsers=ChromeHeadless`
   - Verify no new test failures introduced by your changes

## Build Timing and Timeouts

**NEVER CANCEL BUILDS OR TESTS**. Use these timeout values:

- `npm install`: ~1-5 seconds (timeout: 300 seconds)
- `npm run build`: ~20 seconds (timeout: 3600 seconds)
- `npm test`: ~20-30 seconds (timeout: 1800 seconds)
- `npx ng serve`: ~10 seconds to start (timeout: 600 seconds)
- `dotnet build`: ~10-30 seconds on Windows (timeout: 3600 seconds)

## Repository Structure

```
src/
├── BrowserHost/           # C# WPF application (Windows only)
│   ├── CefInfrastructure/ # CefSharp integration
│   ├── Features/          # Application features
│   ├── chrome-app/        # Angular build output (copied here)
│   └── BrowserHost.csproj # Main .NET project
├── chrome-app/            # Angular application
│   ├── src/app/          # Angular components
│   ├── package.json      # npm dependencies
│   └── angular.json      # Angular configuration
└── Chiaroscuro.sln       # Visual Studio solution
```

### Key Areas for Development

- **Angular UI Components**: `src/chrome-app/src/app/parts/`
- **C# Browser Integration**: `src/BrowserHost/CefInfrastructure/`
- **Application Features**: `src/BrowserHost/Features/`
- **Content Pages**: `src/chrome-app/src/app/content-pages/`

## Limitations and Known Issues

- **.NET Build**: FAILS on Linux/macOS (Windows-only WPF+CefSharp)
- **Angular SSR**: Prerender error during build (non-blocking, build succeeds)
- **Testing**: No .NET unit tests, only Angular tests (27 test files)
- **Linting**: No ESLint/Prettier configured (manual code review required)
- **Full App Testing**: Cannot test complete browser functionality on Linux/macOS

## Code Standards

### Required Before Each Commit
- Angular code compiles without TypeScript errors: `npm run build`
- Angular tests pass: `npm test -- --watch=false --browsers=ChromeHeadless`
- On Windows: .NET code compiles: `dotnet build`

### Angular Guidelines
- Use newest Angular features and syntax
- Never use `*ngFor` or `*ngIf` (use `@for` and `@if`)
- Never use `@Input` or `@Output` (use `input()` and `output()` signal functions)
- Components are standalone by default
- Do not prefix component selectors with `app-`

### C# Guidelines
- Use latest C# language features
- Target .NET 9.0-windows framework
- Follow WPF and CefSharp best practices

### General Guidelines
- Avoid adding unused code
- Never leave "TODO" comments in committed code
- Always provide complete code changes in PR suggestions

## Common Commands Reference

```bash
# Quick build validation
cd src/chrome-app && npm run build && npm test -- --watch=false --browsers=ChromeHeadless

# Start development
cd src/chrome-app && npx ng serve

# Full build sequence
cd src/chrome-app && npm install && npm run build
cd ../BrowserHost && rm -rf chrome-app && cp -r ../chrome-app/dist/chrome-app/browser chrome-app

# On Windows only
cd src/BrowserHost && dotnet build
```

Remember: This repository implements a custom browser with complex UI interactions. Always test user scenarios like tab management, navigation, and keyboard shortcuts when making changes.