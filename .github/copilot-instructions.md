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

## General Coding Guidelines
- Avoid adding code that is not currently being used.
- Always provide finished code. This means that there should be no "TODO" comments in the code.

## Angular Guidelines
- Use new newest Angular features and syntax.
  - Never use `*ngFor` or `*ngIf` (instead use `@for` and `@if`).
  - Never use `@Input` or `@Output` (instead use `input` and `output` signal based functions).
- Make sure there are no type errors in the code.
- Components are standalone by default.
- Do not prefix component selectors with `app-` or any other prefix.

## C# Features
- Use new language features where applicable.

## General PR Review Guidelines
- When suggesting code changes, you MUST include the entire change. For example, if you
are suggesting extracting code to a method, you must include the new method, not just update the call
site as if the method exists.