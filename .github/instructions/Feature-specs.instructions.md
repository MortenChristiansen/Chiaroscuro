---
applyTo: "**/Features/**/*Feature.cs"
---

# Feature specification file conventions

Each backend feature class must have a sibling Markdown spec file describing the feature from a user’s perspective.

## File naming and location

- For a feature file named `XXXFeature.cs`, add a spec file next to it named `XXXFeature.specs.md`.
- Keep the spec in the same folder as the feature, so it is easy to discover while editing the feature.

Examples:

- `src/BrowserHost/Features/ActionContext/Tabs/TabsFeature.cs`
- `src/BrowserHost/Features/ActionContext/Tabs/TabsFeature.specs.md`

## Writing style

- Write in natural language, as if explaining the feature to a user (not as implementation notes).
- Prefer “must/should/may” language for requirements.
- Avoid describing internal classes, events, or APIs unless it’s needed to explain observable behavior.

## Recommended structure

Use this format unless there is a strong reason not to:

- `# Specification for <Feature name>`
- `## Overview`
- `## Terminology` (optional but recommended for clarity)
- `## Requirements`
- `## Workflows`
- `## Interactions`
  - `### Keyboard shortcuts`
  - `### Mouse interactions`

## Keyboard shortcut rules

- The keyboard shortcut list in each spec must match the wording in `README.md` for the shortcuts that the feature owns.
- If the feature does not own any shortcuts, say “None.”
- Do not list shortcuts that belong to other features.

## Avoiding duplication

- Specs should be readable on their own, but avoid copy/pasting large shared explanations.
- Prefer referencing related specs for shared concepts.
  - Example: “See `TabsFeature.specs.md` for the Action Context tab list behavior.”

## Keep it verifiable

- Write requirements so they can be evaluated against the running app.
- Prefer concrete statements over vague descriptions.
  - Good: “Clicking a workspace icon activates that workspace.”
  - Avoid: “Workspaces are easy to switch between.”
