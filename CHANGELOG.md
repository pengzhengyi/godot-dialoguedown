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
