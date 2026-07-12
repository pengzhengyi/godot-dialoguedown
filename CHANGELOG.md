# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## Unreleased

### Added

- **Autocomplete in the Source editor** (Live Edit): as you type, the editor
  suggests names drawn from the document itself — a **jump target** after `](#`, a
  **speaker id** after `@`, a **`#`tag**, or a **speaker** at the start of a line.
  Completed jump anchors match the preview's headings exactly. Suggestions come
  through a symbol-source seam, so the semantic analyzer's resolved symbols can feed
  the same completions later.
- **Architecture tests** ([`tests/DialogueDown.Architecture.Tests`](tests/DialogueDown.Architecture.Tests),
  built on [NetArchTest.eNhancedEdition](https://github.com/NeVeSpl/NetArchTest.eNhancedEdition))
  that guard the project's dependency direction: the engine-agnostic core must not
  depend on the CLI, the visualization projects, or any Spectre/Godot/console
  package; each visualization layer depends only downward; and inside the core the
  Dialogue AST stays decoupled from Markdown while no pipeline stage calls back into
  the compilation orchestrator. Exception types are also kept under `*.Errors`.
- **Collapsible side panels** in the visualization: hide the graph's node-details
  inspector, or the Source tab's preview, to give the graph or editor the full
  width. A hide/show handle sits on each panel's divider — doubling as the
  always-present re-open control once the panel is gone — and the choice is
  remembered across reloads.
- A **full-screen mode** for the visualization: a maximize button in each graph's
  zoom cluster — and on the Source tab — fills the window with the active tab and
  hides the header, tabs, and status bar. Toggle it with the button or <kbd>f</kbd>,
  and leave it with <kbd>f</kbd> or <kbd>Esc</kbd>.
- A **documentation site** built with [DocFX](https://dotnet.github.io/docfx/) and
  published to GitHub Pages from `docs/` on every merge to `main`: a writer
  **Guide**, a **Contributing** section with the per-stage design notes, and a
  generated **C# API reference** — at
  <https://pengzhengyi.github.io/godot-dialoguedown/>. The docs tree is
  reorganized by audience into `docs/guide/`, `docs/contributing/design-notes/`,
  and `docs/demo/`.
- A **live demo** of the visualization report, published to GitHub Pages: an
  interactive, read-only export of a sample script
  ([`examples/gallery.dialogue.md`](examples/gallery.dialogue.md)), rebuilt on
  every merge to `main` at
  <https://pengzhengyi.github.io/godot-dialoguedown/demo/>.
- Script compiler facade: one public `IScriptCompiler.Compile(source)` seam that
  runs the stages (parse → transpile → desugar, deliberately incomplete) and
  returns a `CompilationResult`. Wire it into a container with `AddDialogueDown()`
  (stages registered via `TryAdd`, so any one is swappable) or build it
  container-free with `ScriptCompilerFactory.CreateDefault()`.
- Desugar stage that normalizes the Dialogue AST between the transpiler and
  semantic analysis, behind the `IScriptDesugarer` seam: it assembles a
  single-line jump (`JumpIndicator` + `Link` → `Jump`, degrading a dangling `=>`
  to plain text) and fills a `DefaultSpeaker` on every speaker-less line, wrapping
  the result as a `DesugaredScriptDocument`. Built on a reusable, clone-by-default
  `DialogueAstRewriter`.
- Dialogue AST and the Markdown-to-Dialogue transpiler that turns the parsed
  Markdown into dialogue nodes — speaker/speech lines, flat scene-heading
  markers, choices, inline styling, game calls, tags, and jump indicators —
  behind the `IScriptTranspiler` seam.
- An interactive **`visualize`** report with a runtime **View ⇄ Edit** toggle: a
  read-only, auto-updating **View** and an in-browser CodeMirror **Edit** mode that
  saves back to the file — with search, section folding, Markdown formatting
  shortcuts (bold/italic/link and emphasis auto-surround), and a **System / Light /
  Dark** theme toggle.
- A **Desugared AST** tab in the `visualize` report — the desugarer's normalized
  Dialogue AST as a third graph stage after the Dialogue AST. Synthetic nodes the
  desugarer inserts (a default speaker on a speaker-less line) render as
  "inserted — no source" rather than a blank source block.
- A `visualize <script> --emit mermaid|dot` option that writes each stage's graph
  as portable **Mermaid** or **Graphviz DOT** text to `--output` or standard
  output, for embedding a graph elsewhere. Emitted Mermaid is colored by the same
  cross-stage categories as the interactive report.
- Initial OSS community files and CI configuration.
- Project logo and favicon: a chat-bubble Markdown "M" mark, with an expanded
  variant showing a choice branching into options and scenes.

### Changed

- The CLI `compile` command now runs real compilation through the core
  `IScriptCompiler` facade, replacing the placeholder that reported "not
  implemented".
- `SourceSpan` now allows a zero-width range (`SourceSpan.EmptyAt`, `IsEmpty`) so
  a synthetic node with no source text — such as a filled-in default speaker —
  marks a caret at its position instead of borrowing a neighbor's range.
- `visualize <script>` now opens a **served session** (read-only **View** by default)
  instead of a one-shot static file; the offline snapshot is written with `-o`.
- The `visualize` servers now compress responses (gzip), cutting the report page's
  transfer roughly threefold when it is viewed over a LAN or VPN; the hot-reload SSE
  stream is left uncompressed so events keep streaming.
- Each stage's graph now remembers where you left it across tab switches and
  hot-reloads: an adjusted graph keeps its own zoom, pan, and collapsed branches,
  while an untouched graph inherits the current view so switching tabs stays put.
  Graphs open on a readable, root-centered default, and the zoom toolbar takes a
  typed percentage or a one-click revert.
- The `visualize` report's compilation stages are now sourced through the
  `IScriptCompiler` seam instead of the visualizer wiring the parser and transpiler
  by hand.
- The `visualize` report's **View / Edit** toggle is now frozen on the read-only
  graph tabs (it governs only the Source editor), so it no longer hints that a graph
  is editable.

### Removed

- The `--watch`, `--live`, and `--mode` flags — superseded by the default served
  session (View), `--edit` (start in Edit), and `-o` (static export).

### Fixed

- The brand mark now stays visible on dark backgrounds. Its navy speech bubble used
  to disappear into a dark page; on dark it now inverts to a light bubble with a navy
  mark — across the report and launcher header, the favicon (following the OS color
  scheme), and the Pages demo.
- Escaped inline punctuation (for example `\*`) no longer shifts the source spans
  of the text that follows it. A stripped leading backslash was drifting a
  literal's re-parsed spans by one character; the transpiler now anchors on the
  content's true source position, so spans stay exact for diagnostics and the
  visualizer.
