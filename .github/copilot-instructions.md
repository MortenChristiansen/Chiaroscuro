This is a C# and Typescript based repository for implementing a custom browser. It uses C# and CefSharp as the main application
and uses Angular for the window chrome parts of the UI.

## Code Standards

### Required Before Each Commit
- Make sure that both the frontend and backend code compiles without errors.

### Development Flow
- Build frontend: `npm run build` (in `src/chrome-app/`)
- Build backend: `dotnet build` (in `src/BrowserHost/`)

## Repository Structure
- `src/`: Source code for entire application.
- `src/BrowserHost/`: Source code for the .NET application.
- `src/chrome-app/`: Source code for the Angular application.

## Angular Guidelines
1. Use new newest Angular features and syntax.
2. Make sure there are no type errors in the code.
3. Components are standalone by deautl.

## C# Features
1. Use new language features where applicable.

## General PR Review Guidelines
- When suggesting code changes, you MUST include the entire change. For example, if you
are suggesting extracting code to a method, you must include the new method, not just update the call
site as if the method exists.