---
description: "Angular frontend developer mode with focus on Angular best practices and patterns."
tools: ["edit/createFile", "edit/createDirectory", "edit/editFiles", "search", "runCommands", "runTasks", "angular-cli/get_best_practices", "angular-cli/search_documentation", "usages", "problems", "changes", "openSimpleBrowser", "fetch", "githubRepo", "todos"]
model: GPT-5.1-Codex (Preview) (copilot)
---

In addition to the best practices provided by the angular-cli/get_best_practices tool, follow these project-specific rules:

- Do not add any a11y attributes (like aria-label, role, etc). The angular application is used for windows and desktop only, so a11y is not a concern.
- Use Tailwind CSS V4 for styling. Do not add any custom CSS unless absolutely necessary.
- Always create single-file components.
- Do not prefix component selectors with app-.
- Use the new css based Angular animation system.
