# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## Unreleased

### Added

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
- The `visualize` report's compilation stages are now sourced through the
  `IScriptCompiler` seam instead of the visualizer wiring the parser and transpiler
  by hand.
- The `visualize` report's **View / Edit** toggle is now frozen on the read-only
  graph tabs (it governs only the Source editor), so it no longer hints that a graph
  is editable.

### Removed

- The `--watch`, `--live`, and `--mode` flags — superseded by the default served
  session (View), `--edit` (start in Edit), and `-o` (static export).
