This is a C# + Typescript repository for a Windows-only custom browser.

- **Backend/host:** WPF (.NET) app that hosts browser instances (primarily CefSharp; some tabs can switch to WebView2).
- **Frontend/chrome UI:** Angular app used for the window chrome + overlays (action dialog, context panels, settings UI, etc.).
- **Integration:** The Angular UI is loaded into embedded Chromium instances and communicates with the host via a small JS bridge.

## Repository Structure

- `src/`: Source code for entire application.
- `src/BrowserHost/`: Source code for the .NET application.
- `src/chrome-app/`: Source code for the Angular application.

## High-level Architecture

### Host (WPF)

- `MainWindow` wires up the app by constructing a list of `Feature`s and calling `Configure()` and `Start()`.
- A `Feature` is the primary backend extension point (see `src/BrowserHost/Features/Feature.cs`). Features typically:
  - Subscribe/publish events via `PubSub`.
  - Control embedded UI browsers (e.g. show/hide overlays).
  - Hook keyboard/mouse input via `HandleOnPreviewKeyDown` / `HandleOnPreviewMouseWheel`.

### Tabs

- The main web content is represented by `TabBrowser` (`src/BrowserHost/Tab/TabBrowser.cs`).
- Tabs can be backed by:
  - CefSharp (default) or
  - WebView2 (used for certain SSO/login domains; selection is based on `SettingsFeature.ExecutionSettings`).

### Angular UI hosting

- The Angular app has routes for the chrome and overlays (see `src/chrome-app/src/app/app.routes.ts`).
- In **DEBUG** the host points the embedded UI browsers at the Angular dev server (`http://localhost:4200`).
- In **Release** the host serves the built Angular output via an embedded static server (`ContentServer` in `src/BrowserHost/ContentServer.cs`).
- “Content pages” (e.g. `/settings`) are Angular pages opened inside a _tab_ (not just overlays); the backend keeps a list in `ContentServer.Pages`.

## Frontend ↔ Backend Bridge

There are two directions of communication:

### 1) Frontend → Backend (call C# from Angular)

- C# registers objects into the embedded browser JS context via CefSharp’s `JavascriptObjectRepository`.
  - Every UI browser registers an object named `api` (see `src/BrowserHost/CefInfrastructure/Browser.cs`).
  - Tabs/content pages can register additional named APIs (e.g. `settingsApi`) via `TabBrowser.RegisterContentPageApi(...)`.
- Angular loads these objects using `loadBackendApi()`:
  - See `src/chrome-app/src/app/parts/interfaces/api.ts`.
  - Usage pattern in Angular:
    - `const api = await loadBackendApi<MyApi>('settingsApi')` (or omit name for the default `api`).
    - Call methods directly: `api.saveSettings(...)`, `api.dismissActionDialog()`, etc.

### 2) Backend → Frontend (push events/state into Angular)

- C# calls into Angular by executing a small script via `CallClientApi(...)`.
  - This invokes `window.angularApi.<method>.call(...)` (see `src/BrowserHost/CefInfrastructure/Browser.cs`).
- Angular exposes functions to the backend by registering them into `window.angularApi`:
  - Use `exposeApiToBackend({ methodName: (args...) => { ... } })` from `src/chrome-app/src/app/parts/interfaces/api.ts`.
  - The bridge supports multiple implementations per method name (handy if more than one component listens).

## Implementing New Features

### Add/extend a backend feature (C#)

1. Create a new `Feature` under `src/BrowserHost/Features/<YourFeature>/`.
2. Subscribe/publish events via `PubSub` rather than reaching across features directly.
3. Add the feature to the `_features` list in `MainWindow` so `Configure()`/`Start()` run.
4. If the feature needs a UI surface in Angular:
   - Add or reuse a UI browser (a `Browser<TApi>` subclass) that points at an Angular route, and expose a `BrowserApi` for Angular → C# calls.
   - Use `CallClientApi(...)` (and JSON helpers in `BrowserHost.Utilities`) for C# → Angular updates.

### Add/extend a frontend feature (Angular)

1. Add a route/component under `src/chrome-app/src/app/` (matching existing patterns in `parts/` or `content-pages/`).
2. If the component needs backend calls:
   - Define a TS interface for the API shape.
   - Load the API in `ngOnInit` via `loadBackendApi('apiName')`.
3. If the backend needs to push updates into the component:
   - Register handlers via `exposeApiToBackend({ someUpdate: (payload) => { ... } })`.
   - Ensure the backend calls the same method name via `CallClientApi("someUpdate", ...)`.

### Add a new “content page” (Angular page opened in a tab)

1. Add a route in `src/chrome-app/src/app/app.routes.ts` (e.g. `path: 'my-page'`).
2. Add a matching entry in `ContentServer.Pages` (`src/BrowserHost/ContentServer.cs`).
   - Note: this list is intentionally duplicated (comment in code).
3. If the page needs a dedicated backend API:
   - Create a `BrowserApi` subclass.
   - Register it for that page on tab creation (see the pattern in `src/BrowserHost/Features/Settings/SettingsFeature.cs`).
4. If publishing the app, ensure the static hosting mappings in `ContentServer.CreateWebServer()` cover the route.
