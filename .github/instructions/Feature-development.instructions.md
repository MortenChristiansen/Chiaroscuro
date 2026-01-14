---
applyTo: "**/Features/**/*.cs"
---

# Backend feature development conventions

This file describes how to create new backend features and how to evolve existing ones safely.

It complements:

- `Feature-specs.instructions.md` (how to write and maintain user-facing feature specs)
- `Feature-testing.instructions.md` (how to write backend feature unit tests)

## Core principles

- Features should be small, composable units that communicate via PubSub commands/events. They may use other features directly to retrieve information when more appropriate.
- Prefer observable behavior over internal coupling.
- Keep UI/WPF dependencies behind abstractions so features are testable.

## Do not depend on concrete UI or tab implementations

### Do not use `TabBrowser` directly

- **Do not reference or cast to `TabBrowser` in features.**
- Features must interact with tabs through `ITabBrowser`.
- If additional functionality is needed that currently only exists on `TabBrowser`, **expand `ITabBrowser`** and implement the new member(s) in the concrete tab adapters.

Why:

- Keeps features testable (fake tab implementations can implement `ITabBrowser`).
- Avoids leaking CefSharp/WebView2/WPF specifics into features.

### Do not reference `MainWindow.Instance`

- **Do not call `MainWindow.Instance` from features or feature helpers.**
- Instead, add the needed capability to `IBrowserContext` (or an appropriate window-operations abstraction) and implement it in production via `BrowserContext`.

Why:

- `MainWindow.Instance` makes code hard to test and couples features to the host window.
- `IBrowserContext` is the intended abstraction boundary.

## Creating a new feature

### 1) Create the feature class

- Add a new folder under `src/BrowserHost/Features/<Area>/<YourFeature>/` (or the closest existing grouping). The <area> part is optional.
- Create `XXXFeature.cs` deriving from `Feature`.
- Implement feature behavior by:
  - `PubSub.Handle<TCommand>(...)` for commands (requests)
  - `PubSub.Publish<TEvent>(...)` for events (notifications)
  - `PubSub.Subscribe<TEvent>(...)` for reacting to other features

### 2) Add the feature spec

- Create `XXXFeature.specs.md` next to `XXXFeature.cs`.
- Follow `Feature-specs.instructions.md`.
- Keep shortcuts wording aligned with `README.md` for the shortcuts your feature owns.

### 3) Register the feature in `MainWindow`

- Add any required Browser APIs / window operation dependencies (following existing patterns).
- Add the feature to the `_features` list in `src/BrowserHost/MainWindow.xaml.cs`.
- Order matters:
  - Put foundational features (settings, chrome integration) early.
  - Put features that depend on others after their dependencies.

### 4) Add tests

- Add tests in the `BrowserHost.Tests` project.
- Follow `Feature-testing.instructions.md`.

## Editing an existing feature

### Keep the spec up to date

- If you change externally observable behavior, update the feature’s `*.specs.md` to match.
- If behavior overlaps another feature, prefer referencing the other spec instead of duplicating long explanations.

### Keep behavior covered by tests

- If you change behavior, make sure there is proper test coverage.
- If needed, add new tests to cover the changed behavior and/or adjust existing ones.

### When you need new capabilities

- Add new capability in this order:
  1. Expand `ITabBrowser` if it’s tab behavior.
  2. Expand `IBrowserContext` if it’s host/window state or coordination.
  3. Add a dedicated `*WindowOperations` abstraction if it’s UI surface operations.
- Avoid introducing new direct WPF dependencies in feature code.
