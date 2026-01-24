---
name: feature-test
description: Unit testing for Chiaroscuro features. Use when writing or editing feature tests in BrowserHost.Tests, creating test fixtures, or testing PubSub behavior.
---

# Feature Unit Test Conventions

## Test Names

- **Must be natural English sentences with correct grammar**
- Good: `Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_zooms_in_by_2_points()`
- Good: `Publishing_a_TabDeactivatedEvent_closes_tab_palette_if_it_is_open()`

## Arrange/Act/Assert Formatting

- Implicit AAA structure
- **No section comments** (`// Arrange`, `// Act`, `// Assert`)
- Use **up to 2 empty lines** to visually group phases

## Building Features Under Test

Use `TestBrowserContext.CreateFeature` (globally included):

```csharp
CreateFeature<ZoomFeature>()
    .WithCurrentTab(out var tab, ...)
    .ConfigureContext(ctx => ...)
    .CaptureContext(out var context)
    .Build();
```

Builder methods:
- `WithCurrentTab(out var tab, ...)` / `WithNoCurrentTab()`
- `ConfigureContext(ctx => ...)` for keyboard modifiers, context state
- `CaptureContext(out var context)` for asserting `IBrowserContext` calls

## IBrowserContext Abstraction

- Features use `IBrowserContext`, not `Window`/`MainWindow`
- Tests use `TestBrowserContext` to control inputs and capture outputs

## PubSub

- Already configured per test (no setup needed)
- Publish via captured context: `context.PubSub.Publish(new SomeEvent(...))`
- Capture published events: `PubSubMessages.OfType<TEvent>()`

## Event Args

Use `EventArgHelpers`:
- `CreateMouseWheelEventArgs(delta: ...)`
- `CreateKeyEventArgs(Key. ...)`

## Persistent State

- Use real state manager classes with `MockFileSystem`
- Access via `TestBrowserContext.FileSystem`
- Seed state by writing to fake filesystem or calling state manager APIs

## Cross-Feature Dependencies

```csharp
.IncludeRequiredFeature(b => b.BuildPinnedTabsFeature())
.IncludeRequiredFeature(b => b.BuildPinnedTabsFeature(), out var pinnedTabsFeature)
```

## Assertions

Assert observable behavior:
- Returned `handled` flag
- Calls to collaborators (`tab.SetZoomCalled`, `context.HideTabPaletteCalled`)
- State changes

Keep assertions focused and minimal.
