---
name: feature-dev
description: Backend feature development for Chiaroscuro. Use when creating new features, editing features, or working with files in Features/**/*.cs. Covers feature architecture, PubSub patterns, abstractions.
---

# Backend Feature Development

## Core Principles

- Features are small, composable units communicating via PubSub commands/events
- Prefer observable behavior over internal coupling
- Keep UI/WPF dependencies behind abstractions for testability

## Abstraction Rules

### Never use `TabBrowser` directly
- Interact with tabs through `ITabBrowser`
- Expand `ITabBrowser` if new functionality needed

### Never reference `MainWindow.Instance`
- Add needed capability to `IBrowserContext`
- Implement in `BrowserContext` for production

## Creating a New Feature

### 1. Create feature class
- Add folder under `src/BrowserHost/Features/<Area>/<YourFeature>/`
- Create `XXXFeature.cs` deriving from `Feature`
- Use PubSub patterns:
  - `PubSub.Handle<TCommand>(...)` for commands
  - `PubSub.Publish<TEvent>(...)` for events
  - `PubSub.Subscribe<TEvent>(...)` for reactions

### 2. Add feature spec
- Create `XXXFeature.specs.md` next to `XXXFeature.cs`
- Use `/feature-spec` skill for guidance

### 3. Register in MainWindow
- Add to `_features` list in `MainWindow.xaml.cs`
- Order: foundational features first, dependent features after

### 4. Add tests
- Add in `BrowserHost.Tests` project
- Use `/feature-test` skill for guidance

## Editing Existing Features

- Update `*.specs.md` if changing observable behavior
- Ensure test coverage for changed behavior
- Add new capabilities in order:
  1. `ITabBrowser` for tab behavior
  2. `IBrowserContext` for host/window state
  3. `*WindowOperations` for UI surface operations
- Avoid new direct WPF dependencies
