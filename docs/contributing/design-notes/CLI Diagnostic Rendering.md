# Implementation note: CLI diagnostic rendering

> [!NOTE]
> Status: **proposed / draft** — Component 5 of the diagnostics subsystem
> ([#43](https://github.com/pengzhengyi/godot-dialoguedown/issues/43)), awaiting review.
> Components 1–4 already **collect** located diagnostics and recover; this component makes
> them **visible** to script authors on the `dialoguedown` CLI, and exposes the compilation
> mode there ([#110](https://github.com/pengzhengyi/godot-dialoguedown/issues/110)). It builds
> on the [Diagnostics and Validation](Diagnostics%20and%20Validation.md) note (DD6, DD7, DD9).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Components](#components)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
  - [DR1 — A public, located diagnostic view; internals stay internal](#dr1--a-public-located-diagnostic-view-internals-stay-internal)
  - [DR2 — The engine locates and renders text; the CLI owns presentation](#dr2--the-engine-locates-and-renders-text-the-cli-owns-presentation)
  - [DR3 — One `source(line,column)` location language](#dr3--one-sourcelinecolumn-location-language)
  - [DR4 — A `LineMap` value object, built once per compile](#dr4--a-linemap-value-object-built-once-per-compile)
  - [DR5 — Exit codes follow sysexits](#dr5--exit-codes-follow-sysexits)
  - [DR6 — `--mode` selects a collecting compilation mode](#dr6----mode-selects-a-collecting-compilation-mode)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Recorded decisions](#recorded-decisions)

## Goal and scope

Turn the compiler's collected, offset-based diagnostics into a **public, located,
human-readable view**, render that view as **errata** on the `dialoguedown compile` command —
every problem at once, each pointing at `source(line,column)` — set a meaningful process **exit
code**, and let the author choose the **compilation mode** with `--mode`.

Today `CompilationResult.Diagnostics` is internal and `compile` ignores it: a script with a bad
jump or a duplicate anchor still exits `0` and prints nothing. This component closes that gap so
authoring against DialogueDown gives real feedback.

**In scope:** offset→line/column mapping; a public diagnostic view carrying the rendered message
and location; the CLI errata renderer and its data-error exit code; a `compile --mode` option.

**Out of scope (deferred):** the LSP and web-report projections (Component 6,
[#121](https://github.com/pengzhengyi/godot-dialoguedown/issues/121)); the config-file `mode`
key and the visualization mode tab (the rest of
[#110](https://github.com/pengzhengyi/godot-dialoguedown/issues/110)); a `--mode fail-fast` CLI
option (needs a public fail-fast reporting seam — see DR6); promoting warnings to errors (a
planned per-run toggle).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Located diagnostic** | one collected `Diagnostic` resolved to a `source(line,column)` position and a final message string — the public unit a consumer renders. |
| **Errata** | the CLI's rendering of the located diagnostics for one compile: a sorted list plus a summary. Rendering stays confined to the CLI (per the umbrella note's DD7). |
| **`LineMap`** | the value object that turns a source **offset** into a one-based **line and column**. |
| **`source(line,column)`** | the shared location format, already used for configuration errors (`ConfigurationSourceLocation`) — reused here so a script location reads the same as a config location. |

## Components

The work splits into two cleanly bounded passes; 5a has no CLI dependency and 5b consumes it.

- **5a — the located diagnostic view (engine-side).** A `LineMap`, message rendering (fill the
  descriptor's format with its arguments), and a **public** projection exposed on
  `CompilationResult`. Engine-agnostic, so the CLI now and the LSP/web later share one view.
- **5b — the CLI errata renderer (CLI-side).** The `compile` command renders the view through
  Spectre.Console, sorted by position with a summary line, returns a data-error exit code when the
  report has errors, and gains a `--mode` option.

## Functionality checklist

- [ ] Map any source offset to a one-based `(line, column)`; a span to a start/end position.
- [ ] Expose a public, immutable located-diagnostic view on `CompilationResult` (code, severity,
      message, location) without leaking internal `Diagnostic`/`SourceSpan`/enums.
- [ ] Render each diagnostic as `source(line,column): severity CODE: message`, errors red,
      warnings yellow, info cyan, sorted by position then code, with user text safely escaped.
- [ ] Print a summary line (e.g. `2 errors, 1 warning`); print nothing for a clean compile.
- [ ] Return `Success` for no errors (warnings/info still succeed), `DataError` when errors exist;
      align malformed-config errors to `DataError` too.
- [ ] `compile --mode <stage-boundary|best-effort>` overrides `CompilerOptions.Mode` only when
      given (else inherit the resolved mode); an invalid value is a usage error.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `LineMap` (value object, engine) | precompute line starts from source; `Locate(offset) → LinePosition`, `Locate(SourceSpan) → SourceRange` | `SourceSpan`, `LinePosition` |
| `LinePosition` (public readonly struct) | a one-based `(Line, Column)`; `ToString()` → `line,column` | — |
| `LocatedDiagnostic` (public record) | one located diagnostic: `Code`, `Severity`, `Message`, `Start`, `End` | `LinePosition`, `DiagnosticSeverity` (public) |
| `CompilationResult.Report` (public) | the located diagnostics for the compile, projected once from the internal bag | `LineMap`, `LocatedDiagnostic` |
| `ErrataRenderer` (CLI) | write a `Report` to an `IAnsiConsole`: sorted lines + summary | `IAnsiConsole`, `LocatedDiagnostic` |
| `CompileCommand` (CLI) | compile, render errata, choose the exit code; parse `--mode` | `ErrataRenderer`, `CompilerOptions` |

```mermaid
flowchart LR
    SRC["script source"] --> C["IScriptCompiler.Compile"]
    C --> R["CompilationResult<br/>(internal Diagnostics)"]
    R -- "project once via LineMap" --> V["Report: IReadOnlyList&lt;LocatedDiagnostic&gt;<br/>(public, located, rendered)"]
    V --> CLI["ErrataRenderer → IAnsiConsole"]
    V --> LSP["LSP / web (Component 6, later)"]
    R --> X["exit code by severity"]
```

## Key design decisions

### DR1 — A public, located diagnostic view; internals stay internal

`Diagnostic`, `SourceSpan`, and `DiagnosticDescriptor` are internal and, per the existing
`CompilationResult` remark, "still under active design." Rather than make them public, project to
a small, stable public **`LocatedDiagnostic`** (code string, public severity, rendered message,
line/column start/end). Consumers depend on the projection, not the evolving internals — the same
seam serves the CLI now and the LSP/web later (Component 6). One internal type is deliberately
**promoted to public**: `DiagnosticSeverity` (`Error`/`Warning`/`Info`), because it *is* the view's
contract; its members get explicit numeric values so ordering is a documented, stable API.
`Diagnostic`, `SourceSpan`, and the descriptor stay internal. `Report` returns an
**immutable** snapshot (an array-backed `IReadOnlyList<LocatedDiagnostic>`), projected once.

### DR2 — The engine locates and renders text; the CLI owns presentation

Message composition (filling `MessageFormat` with `MessageArguments`) and offset→line/column
resolution happen **once, in the engine's projection**, so every consumer shares identical text
and locations. Formatting uses `CultureInfo.InvariantCulture` — the report is invariant-English
for now, and culture becomes an explicit projection input if localization lands — so "identical
text" does not silently depend on the host's ambient culture. The CLI adds only **presentation**:
layout, color, sorting, and the summary. This honors the umbrella note's DD7 (errata rendering
confined to the CLI) while keeping the shared substance engine-side, so the LSP and web overlays
do not each re-implement formatting.

### DR3 — One `source(line,column)` location language

Configuration errors already render as `source(line,column)` through the public
`ConfigurationSourceLocation`. Script diagnostics reuse that exact shape so a location reads the
same wherever it appears. `LinePosition.ToString()` yields `line,column`; the CLI prefixes the
source name to form `source(line,column)`.

### DR4 — A `LineMap` value object, built once per compile

`LineMap` precomputes the offsets of each line start (offset `0`, then each offset immediately
after a `\n`) and binary-searches an offset to its line; the column is `offset − lineStart + 1`.
It is built once from `result.Source` during projection and reused — O(n) to build, O(log n) per
lookup. Precise, testable semantics:

- **Valid offsets are `0..source.Length`** (inclusive of the end-of-source insertion position, so
  a zero-width synthetic span at EOF maps). An offset `< 0` or `> Length` is a **broken compiler
  span**: `LineMap` throws rather than clamping, so the bug surfaces instead of hiding.
- **Line and column are one-based**, counted in UTF-16 code units (matching how spans index the
  string, and the LSP convention).
- **`\n` ends a line**; the next line starts at the following offset. A `\r` is an **ordinary
  character** on its line, so in a `\r\n` pair the `\r` is that line's last column and the `\n` is
  the newline — a diagnostic almost never points at either, but the rule is total and unambiguous.
- **End of source:** `"abc"` → offset 3 is `(1,4)`; `"abc\n"` → offset 4 is `(2,1)`; `""` → offset
  0 is `(1,1)`.

### DR5 — Exit codes follow sysexits

`ExitCodes` gains `DataError = 65` (EX_DATAERR): "the input data was incorrect." `compile` returns
it when the report contains an error, `Success` otherwise (a warning-only compile still succeeds).
For consistency, a malformed-configuration error — also invalid input — moves from the generic
`Error` (1) to `DataError`, so every "your input is wrong" outcome shares one code. Genuine
unexpected faults keep `Error`.

### DR6 — `--mode` selects a collecting compilation mode

`compile` gains `--mode <stage-boundary|best-effort>` (kebab-case). It is a **nullable override**:
omitted, the command inherits the resolved `CompilerOptions.Mode` (so a future config-file `mode`
is not clobbered); given, it sets `CompilerOptions.Mode`. An unknown value is a usage error.

**Fail-fast is intentionally not a CLI mode.** Fail-fast is an *embedding* contract — it throws a
`DiagnosticException` at the first error for a host that wants to stop immediately — which is at
odds with errata, whose whole purpose is to render the collected set. The exception is internal
and the CLI has no visibility into it, so surfacing it well would need a public exception seam;
that is deferred (a tracked follow-up) rather than bolted on here. The two collecting modes cover
the CLI's needs: `stage-boundary` (default) stops at the first stage that erred, `best-effort`
runs every stage. `visualize` is unaffected — it forces best-effort by design.

## Error and boundary cases

- **Zero-width (synthetic) span:** start equals end; the CLI shows a single `source(line,column)`
  caret position, not a range.
- **Offset past `source.Length`:** a broken compiler span — `LineMap` throws (see DR4), never
  clamps, so the defect is not silently mislocated.
- **Multi-line span:** the report carries both start and end positions; the CLI shows the start.
- **`\r\n` vs `\n`:** columns count UTF-16 code units after the last line start; a `\r` occupies a
  column on its line and `\n` ends it (see DR4).
- **Empty source:** one line, column 1; an empty script yields no diagnostics.
- **A message containing `[` or `]`:** rendered through Spectre's *interpolated* markup, which
  escapes interpolated values, so a diagnostic's own text can never inject console markup.
- **No diagnostics:** the errata renderer prints nothing and `compile` returns `Success`.
- **Warnings (or info) only:** rendered in their color; exit stays `Success` because `HasErrors`
  is false.

## Integration

- **`CompilationResult`** gains a public `Report` projected from the internal bag using a `LineMap`
  built from `Source`; the internal `Diagnostics` list stays internal.
- **`CompileCommand`** compiles, hands the `Report` to `ErrataRenderer`, then returns
  `DataError` when `HasErrors`, else `Success` — replacing today's unconditional `Success`.
- **`CompileSettings`** gains `--mode`; `CompileCommand` maps it onto `CompilerOptions.Mode`.
- **`CliServices`** registers `ErrataRenderer` (or it is a static writer over `IAnsiConsole`).

## Testability

- **`LineMap`** (unit): offsets across lines, first/last columns, `\r\n`, empty source, the
  end-of-source positions from DR4 (`"abc"`→`(1,4)`, `"abc\n"`→`(2,1)`), a throw for offsets past
  `Length`, and span→range.
- **`LocatedDiagnostic` projection** (unit): the result exposes located reports whose messages are
  the descriptors' formats filled with their arguments (invariant culture), at the right positions.
- **`ErrataRenderer`** (unit, Spectre `TestConsole`): a fixed report renders sorted lines with the
  right colors and a correct summary; a message containing `[`/`]` is escaped, not interpreted; an
  empty report writes nothing.
- **`CompileCommand`** (integration, `CommandAppTester`): a clean script → `Success`, no errata; an
  error script → errata plus `DataError`; a warning-only script → errata plus `Success`; `--mode`
  threads to the compile and, when omitted, inherits the resolved mode; an invalid mode → usage
  error.

## Recorded decisions

Resolved while finalizing the design (headless), including the design-review pass:

1. **Naming.** The public view type is **`LocatedDiagnostic`** (matching the ubiquitous language),
   exposed as **`CompilationResult.Report`** — the compile's diagnostic report. `Errata` /
   `ErrataRenderer` name the CLI rendering.
2. **Sort key.** Order by **position then code** — a compiler-like reading order — over
   severity-first.
3. **Config `mode` key.** **Deferred** to the rest of
   [#110](https://github.com/pengzhengyi/godot-dialoguedown/issues/110), keeping this component
   CLI-focused and cleanly bounded.
4. **Fail-fast is not a CLI mode** (DR6). It is a throwing embedding contract, incompatible with
   errata and invisible to the CLI (internal exception); a public fail-fast reporting seam is a
   tracked follow-up. `--mode` offers the two collecting modes.
5. **Public severity, invariant text** (DR1/DR2). `DiagnosticSeverity` is promoted to public with
   explicit values as the view's contract; messages render under `InvariantCulture`. `Diagnostic`,
   `SourceSpan`, and the descriptor stay internal.
6. **Consistent data-error exit** (DR5). Malformed configuration joins script errors under
   `DataError` (65), so every invalid-input outcome shares one exit code.
