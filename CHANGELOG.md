# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## Unreleased

### Added

- Dialogue AST and the Markdown-to-Dialogue transpiler that turns the parsed
  Markdown into dialogue nodes — speaker/speech lines, flat scene-heading
  markers, choices, inline styling, game calls, tags, and jump indicators —
  behind the `IScriptTranspiler` seam.
- An interactive **`visualize`** report with a runtime **View ⇄ Edit** toggle: a
  read-only, auto-updating **View** and an in-browser CodeMirror **Edit** mode that
  saves back to the file — with search, section folding, Markdown formatting
  shortcuts (bold/italic/link and emphasis auto-surround), and a **System / Light /
  Dark** theme toggle.
- Initial OSS community files and CI configuration.

### Changed

- `visualize <script>` now opens a **served session** (read-only **View** by default)
  instead of a one-shot static file; the offline snapshot is written with `-o`.

### Removed

- The `--watch`, `--live`, and `--mode` flags — superseded by the default served
  session (View), `--edit` (start in Edit), and `-o` (static export).
