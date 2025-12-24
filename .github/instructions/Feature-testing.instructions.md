---
applyTo: "**/Features/*Test.cs"
---

# Backend Feature unit test conventions

Feature tests follow a small set of conventions designed to keep tests readable, isolated, and free of WPF/Dispatcher dependencies.

## Test names

- Test method names **must be natural English sentences** with **correct grammar**.
- Prefer descriptive phrasing over abbreviations.
  - Good: `Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_zooms_in_by_2_points()`
  - Good: `Publishing_a_TabDeactivatedEvent_closes_tab_palette_if_it_is_open()`

## Arrange / Act / Assert formatting (no labels)

- Tests are written in an implicit Arrange/Act/Assert structure.
- **Do not add section comments** like `// Arrange` / `// Act` / `// Assert`.
- The **only** indicator of the structure is whitespace:
  - Use **up to 2 empty lines** to visually group the three phases.
  - Typically this means one empty line between the setup, the operation, and the assertions.

## Building features under test

- Prefer using the builder helper `TestBrowserContext.CreateFeature` to construct the feature.
- Use the builder to configure state:
  - `WithCurrentTab(out var tab, ...)` / `WithNoCurrentTab()`
  - `ConfigureContext(ctx => ...)` for keyboard modifiers and other context state
  - `CaptureContext(out var context)` when you need to assert calls made via `IBrowserContext`

## `IBrowserContext` is the MainWindow abstraction

- Features should avoid talking directly to `MainWindow` for environment state and UI operations.
- Instead, treat `IBrowserContext` as the abstraction over `MainWindow`:
  - In production, `BrowserContext` delegates to `MainWindow` and other WPF primitives.
  - In tests, `TestBrowserContext` is used to precisely control inputs and capture outputs.
- This is the same pattern used by `ZoomFeature` and `TabPaletteFeature`.

## PubSub is already configured per test

- Tests do **not** need to set up `PubSub`.
- The assembly applies a per-test PubSub scope automatically via `PerTestPubSubContextAttribute`, ensuring:
  - Isolation between tests (no subscriber leakage)
  - A direct dispatch strategy (no UI thread/Dispatcher requirement)

## Event argument creation

- Use the helpers in `EventArgHelpers` to create WPF event args without requiring a real input device:
  - `CreateMouseWheelEventArgs(delta: ...)`
  - `CreateKeyEventArgs(Key. ...)`

## Assertions

- Assert the observable behavior:
  - The returned `handled` flag where relevant.
  - Calls made to collaborators (e.g., `tab.SetZoomCalled`, `context.HideTabPaletteCalled`).
  - State changes (e.g., updated zoom level).
- Keep assertions focused and minimal; avoid asserting internal state unless it is exposed via the test context.
