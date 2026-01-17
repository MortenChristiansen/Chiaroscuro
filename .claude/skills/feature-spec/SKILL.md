---
name: feature-spec
description: Feature specification files for Chiaroscuro. Use when creating or editing *.specs.md files, documenting feature behavior, or writing user-facing feature documentation.
---

# Feature Specification Conventions

Each backend feature must have a sibling spec file describing it from user perspective.

## File Naming

- For `XXXFeature.cs`, create `XXXFeature.specs.md` in same folder
- Example: `src/BrowserHost/Features/ActionContext/Tabs/TabsFeature.specs.md`

## Writing Style

- Natural language, explaining to a user (not implementation notes)
- Use "must/should/may" for requirements
- Avoid internal classes/events/APIs unless needed for observable behavior

## Structure

```markdown
# Specification for <Feature name>

## Overview

## Terminology (optional)

## Requirements

## Workflows

## Interactions

### Keyboard shortcuts

### Mouse interactions
```

## Keyboard Shortcut Rules

- Must match wording in `README.md` for shortcuts feature owns
- If no shortcuts, say "None."
- Don't list shortcuts belonging to other features

## Avoiding Duplication

- Reference related specs for shared concepts
- Example: "See `TabsFeature.specs.md` for Action Context tab list behavior."

## Keep It Verifiable

- Write requirements evaluable against running app
- Concrete statements over vague descriptions
- Good: "Clicking a workspace icon activates that workspace."
- Bad: "Workspaces are easy to switch between."
