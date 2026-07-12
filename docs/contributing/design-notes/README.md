# Implementation notes

Design and rationale notes for DialogueDown's compiler. Each note covers one
component; this README indexes them and records **cross-cutting conventions**
that every component shares — starting with the error model.

> [!NOTE]
> The error model below is a **proposed design**, not yet implemented. It defines
> the exception types and message conventions each component will adopt as it is
> built. Component notes link back here rather than redefining errors locally.

## Table of contents

- [Notes in this folder](#notes-in-this-folder)
- [Error model](#error-model)
  - [Principles](#principles)
  - [Exception hierarchy](#exception-hierarchy)
  - [What each stage raises](#what-each-stage-raises)
  - [Domain errors vs usage errors](#domain-errors-vs-usage-errors)
  - [Every error carries a location](#every-error-carries-a-location)
  - [Message conventions](#message-conventions)
  - [Error codes (optional, future)](#error-codes-optional-future)
  - [Open choices](#open-choices)

## Notes in this folder

| Note | Component | Status |
| --- | --- | --- |
| [Markdown Front-End](./Markdown%20Front-End.md) | Source text → Markdown AST (Markdig adapter) | Implemented |
| [Unmodeled Markdown Handling](./Unmodeled%20Markdown%20Handling.md) | How unmodeled Markdown nodes are ignored vs kept as raw text | Implemented |
| [Compilation Visualization](./Compilation%20Visualization.md) | Compiler-stage IRs → interactive diagrams (Markdown AST today) | Implemented |
| [Live Visualization — Hot Reload](./Live%20Visualization%20-%20Hot%20Reload.md) | Watch a script and hot-reload the report from a local server | Implemented |
| [Live Visualization — File Launcher](./Live%20Visualization%20-%20File%20Launcher.md) | Browse and open a script in the launcher (the uniform `visualize` entry point) | Implemented |
| [Live Visualization — Live Edit](./Live%20Visualization%20-%20Live%20Edit.md) | Edit the source in the report; compile-as-you-type and save to disk | Implemented |
| [Live Visualization — View and Edit Modes](./Live%20Visualization%20-%20View%20and%20Edit%20Modes.md) | Unify watch/live into a served session with a runtime View⇄Edit toggle; static becomes an export | Implemented |
| [Markdown to Dialogue AST Transpiler](./Markdown%20to%20Dialogue%20AST%20Transpiler.md) | Markdown AST → Dialogue AST | Implemented |
| [Desugar](./Desugar.md) | Dialogue AST → normalized Dialogue AST (jump assembly, default speaker) | Implemented |
| [Script Compiler Facade](./Script%20Compiler%20Facade.md) | One `IScriptCompiler` seam over the stages + `AddDialogueDown` DI | Implemented |
| [Command-Line Interface](./Command-Line%20Interface.md) | The `dialoguedown` CLI: `compile` + `visualize` (Spectre.Console.Cli) | Implemented |
| [Visualize on the CLI](./Visualize%20on%20the%20CLI.md) | Wire `dialoguedown visualize` to the engine; retire the hand-rolled CLI | Implemented |
| [Compiler Stage Tooltips](./Compiler%20Stage%20Tooltips.md) | Per-stage hover tips on the report tabs, from a description on each stage | Implemented |
| [Dialogue AST Visualization Tab](./Dialogue%20AST%20Visualization%20Tab.md) | The transpiler's Dialogue AST as a second graph tab in the report | Implemented |
| [Desugared AST Visualization Tab](./Desugared%20AST%20Visualization%20Tab.md) | The desugarer's normalized AST as a third tab (via the `IScriptCompiler` seam); synthetic nodes render as inserted; the toggle freezes on read-only tabs | Implemented |
| [Visualize CLI — Emit Mermaid and DOT](./Visualize%20CLI%20-%20Emit%20Mermaid%20and%20DOT.md) | `visualize --emit mermaid\|dot` emits each stage's graph as portable text (colored Mermaid); no in-report rendering | Implemented |
| [Graph Position Preservation](./Graph%20Position%20Preservation.md) | Hybrid per-graph zoom/pan/fold memory (adjusted graphs pin their own, untouched inherit the current view), a root-centered default, and an editable zoom with revert | Implemented |
| [Source Editor Autocompletion](./Source%20Editor%20Autocompletion.md) | Document-aware editor completions (jump targets, speakers, `@id`s, `#tag`s) behind a symbol-source seam for future semantic symbols | In progress |

## Error model

The compiler runs in stages: **source → Markdown AST → Dialogue AST → graph →
runtime**. A fault can occur at any stage, and callers (a game integrating the
library) need to tell *what kind* of fault happened and *where*. The error model
makes that explicit through **distinct exception types per stage and kind**, each
carrying a clear, actionable message.

### Principles

- **Two axes.** Classify every fault by **kind** (is the input malformed, or
  well-formed but meaningless?) and by **layer/language** (which stage/DSL raised
  it). The type name encodes both, e.g. `MarkdownSyntaxError` vs
  `DialogueSyntaxError` vs `DialogueSemanticError`.
- **Syntax ≠ semantics.** A `SyntaxError` means the text could not be understood
  structurally. A `SemanticError` means it parsed fine but violates meaning
  (unknown speaker, dangling jump). They are different types, so callers and tools
  can react differently.
- **One base to catch them all.** Every domain fault derives from a single base
  (`DialogueDownException`), so a caller can `catch` broadly or narrowly.
- **Locate everything.** Every compilation error carries a
  [`SourceSpan`](./Markdown%20Front-End.md#the-markdown-ast-model) so messages and
  tooling can point at the exact offending characters.
- **Fail with intent.** Messages state what is wrong, where, and how to fix it —
  never a bare "parse error".
- **Usage errors are not domain errors.** Programmer mistakes (a `null` argument,
  an out-of-range value) use standard .NET exceptions and stay outside this
  hierarchy (see [Domain errors vs usage errors](#domain-errors-vs-usage-errors)).

### Exception hierarchy

```mermaid
classDiagram
    class DialogueDownException {
        <<abstract>>
    }
    class ScriptCompilationException {
        <<abstract>>
        +SourceSpan Span
    }
    class SyntaxError {
        <<abstract>>
    }
    class SemanticError {
        <<abstract>>
    }
    class MarkdownSyntaxError
    class DialogueSyntaxError
    class DialogueSemanticError

    DialogueDownException <|-- ScriptCompilationException
    ScriptCompilationException <|-- SyntaxError
    ScriptCompilationException <|-- SemanticError
    SyntaxError <|-- MarkdownSyntaxError
    SyntaxError <|-- DialogueSyntaxError
    SemanticError <|-- DialogueSemanticError
```

- **`DialogueDownException`** — abstract base for everything the library throws
  as a domain fault. Callers catch this to handle "any DialogueDown error".
- **`ScriptCompilationException`** — abstract; a fault while compiling a script.
  Carries the `SourceSpan` (and, derived from it, a line/column) that locates the
  problem. A future **runtime** branch (graph execution faults) can hang directly
  off `DialogueDownException`, parallel to this one.
- **`SyntaxError`** — abstract; structurally malformed input.
- **`SemanticError`** — abstract; well-formed input that breaks a rule.
- The concrete leaves are what each stage actually throws (next section).

### What each stage raises

| Stage | Type | Kind | Example |
| --- | --- | --- | --- |
| Markdown front-end | `MarkdownSyntaxError` | Syntax | Reserved for unrecoverable Markdown parse faults. The front-end is deliberately permissive — it **flattens** unmodeled constructs to raw text rather than failing — so this is rare in practice. |
| Transpiler (DSL grammar) | `DialogueSyntaxError` | Syntax | A jump `=>` not followed by a Markdown link; a malformed tag (`#` with no name); a code-span command that is not valid query/command grammar. |
| Reference validation / compile | `DialogueSemanticError` | Semantic | Unknown speaker reference; dangling jump (anchor/file does not exist); conflicting speaker metadata for the same speaker; duplicate section anchor; unknown reserved tag (`##foo`). |

Each stage owns its type: the Markdown adapter never throws a `DialogueSyntaxError`,
and the transpiler never throws a `MarkdownSyntaxError`. This keeps the failing
layer unambiguous from the type alone.

### Domain errors vs usage errors

The hierarchy above is for **faults in the script being compiled** — bad *input*.
It is **not** for **programmer mistakes** calling the API. Those stay as standard
.NET exceptions:

| Situation | Exception | Why |
| --- | --- | --- |
| `null` passed where a value is required | `ArgumentNullException` | Caller contract violation, not a script fault. |
| An AST invariant is violated in code (e.g. a heading level outside 1–6, an empty text run, a non-positive span length) | `ArgumentException` / `ArgumentOutOfRangeException` | Internal construction bug, caught in tests, not something a script author can cause. |

Rule of thumb: if a **script author** can trigger it by writing a bad `.dialogue.md`,
it is a `DialogueDownException`. If only a **developer** can trigger it by
misusing the API, it is a standard argument/usage exception.

### Every error carries a location

`ScriptCompilationException` exposes the `SourceSpan` of the offending text. Since
`SourceSpan` is a start-offset + length into the original source, a line/column
(and a source snippet) can be derived from it for display. Messages should render
that location so authors can jump straight to the problem.

### Message conventions

A good message answers three questions: **what** is wrong, **where**, and **how**
to fix it.

- Lead with the problem, not the mechanics ("jump target not found", not
  "null reference in ResolveJump").
- Include the **offending token** and its **line:column**.
- Suggest a fix or show the expected form when it is short.
- Use plain language a script author understands; avoid internal type names.

| Kind | Weak | Strong |
| --- | --- | --- |
| `DialogueSyntaxError` | `Invalid jump.` | `Syntax error at 12:5 — a jump must be '=> [label](target)'; found '=> play-tennis' with no link.` |
| `DialogueSemanticError` | `Unknown speaker.` | `Unknown speaker 'Alicia' at 8:1 — declare it inline or add it to speakers.json (did you mean 'Alice'?).` |
| `DialogueSemanticError` | `Bad jump.` | `Jump target '#play-tennis' at 3:4 does not match any section heading in this file.` |

### Error codes (optional, future)

For tooling (editor squiggles, documentation, suppression), errors may later carry
a stable **code** alongside the message, e.g. `DLG1001` for "malformed jump". This
is out of scope for the first implementation; the type + message are sufficient
until an editor integration needs stable identifiers.

### Open choices

Flagged for confirmation before implementation:

- **Base type name.** `DialogueDownException` (matches the library name) vs a
  shorter `DialogueException`.
- **Intermediate `ScriptCompilationException`.** Keep it as the span-carrying
  parent (recommended, leaves room for a runtime branch), or fold `Span` directly
  onto `SyntaxError`/`SemanticError` and drop the middle layer.
- **Error codes.** Adopt a code scheme now, or defer until tooling needs it
  (deferred above).
