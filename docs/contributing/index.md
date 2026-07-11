# Contributing

Developer-facing documentation for **working on DialogueDown** itself — its
architecture, the reasoning behind each compiler stage, and how to get set up.

## Start here

- **[Contribution guide](https://github.com/pengzhengyi/godot-dialoguedown/blob/main/CONTRIBUTING.md)**
  — how to report issues, develop, test, and open pull requests.
- **[Code of conduct](https://github.com/pengzhengyi/godot-dialoguedown/blob/main/CODE_OF_CONDUCT.md)**
  and **[security policy](https://github.com/pengzhengyi/godot-dialoguedown/blob/main/SECURITY.md)**.

## Understand the design

- **[Design notes](design-notes/README.md)** — one note per component and compiler
  stage (the Markdown front-end, the transpiler, desugaring, the visualization,
  the CLI, and more), each recording the goal, key decisions, and tradeoffs.
- **[API reference](../api/index.md)** — the generated C# API, useful when reading
  or extending the library.

## The compiler pipeline

DialogueDown lowers a script through distinct stages, each with its own design
note under [Design notes](design-notes/README.md):

**source → Markdown AST → Dialogue AST → desugared AST → (semantic analysis →
graph → runtime, in progress)**.

Reading the notes in that order is the fastest way to learn how the compiler fits
together before making a change.
