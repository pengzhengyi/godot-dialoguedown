# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## Unreleased

### Added

- **Compiler pipeline behind one `IScriptCompiler` facade** — compiles a Markdown
  dialogue script through parse → transpile → desugar → semantic analysis: it builds
  a Dialogue AST, normalizes it (assembling jumps and filling default speakers), and
  resolves speakers, scenes, and jumps into a validated semantic model, reporting
  invalid references. Wire it up with `AddDialogueDown()` (DI) or
  `ScriptCompilerFactory.CreateDefault()`. See the
  [design notes](docs/contributing/design-notes/README.md).
- **`dialoguedown` CLI** — `compile` runs the compiler pipeline; `visualize` opens
  the interactive report or writes a stage's graph as portable **Mermaid** or
  **Graphviz DOT** text (`--emit`).
- **Interactive `visualize` report** — explore each compiler stage as a graph — the
  Markdown, Dialogue, and Desugared ASTs, and the **semantic model** shown as a scene
  tree beside cross-linked speaker, anchor, and jump-resolution tables — with a runtime
  **View ⇄ Edit** toggle: a read-only, auto-updating **View** and an in-browser editor
  that saves back to the file — with search, section folding, Markdown formatting
  shortcuts, document-aware autocomplete, and a **light/dark** theme.
- **`visualize` report navigation** — collapsible side panels, a full-screen mode,
  and per-graph position memory that keeps each stage's zoom, pan, and collapsed
  branches across tab switches and hot-reloads.
- **Documentation site and live demo** — a
  [DocFX site](https://pengzhengyi.github.io/godot-dialoguedown/) (a writer Guide, a
  Contributing section with the design notes, and a generated C# API reference) and a
  [live, read-only demo](https://pengzhengyi.github.io/godot-dialoguedown/demo/) of
  the report, both published to GitHub Pages on every merge to `main`.
- **Development guardrails** — architecture tests that enforce the project's
  dependency direction (the engine-agnostic core stays free of the CLI, the
  visualization projects, and any engine/console packages), plus build-time size and
  complexity limits on the core library.
- **Project logo and favicon** — a chat-bubble Markdown "M" mark.
- Initial OSS community files and CI.

### Changed

- `visualize <script>` now opens a **served session** (read-only **View** by default)
  instead of a one-shot static file; write an offline snapshot with `-o`.
- The `visualize` servers now compress responses (gzip), cutting the report's
  transfer roughly threefold over a LAN or VPN; the hot-reload stream stays
  uncompressed so events keep streaming.
- `SourceSpan` now allows a zero-width range (`SourceSpan.EmptyAt`, `IsEmpty`) so a
  synthetic node with no source text — such as a filled-in default speaker — marks a
  caret at its position instead of borrowing a neighbor's range.

### Removed

- The `--watch`, `--live`, and `--mode` flags — superseded by the default served
  session (View), `--edit` (start in Edit), and `-o` (static export).

### Fixed

- The brand mark now stays visible on dark backgrounds — it inverts to a light
  bubble across the report and launcher, the favicon, and the demo.
- Escaped inline punctuation (for example `\*`) no longer shifts the source spans of
  the text that follows it, so spans stay exact for diagnostics and the visualizer.
