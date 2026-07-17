# Implementation note: CLI configuration

> [!NOTE]
> Status: **approved — implementation in progress**. Threads a project's
> [`CompilerOptions`](./Configuration.md) — built from a `dialogue.toml` by the
> [configuration loader](./Configuration%20Loader.md) — through the `dialoguedown`
> CLI, so `compile` and `visualize` honor configured speakers. Because the report's
> autocompletion already derives its speaker symbols from the compiled semantic
> model, configured speakers reach the editor's completions for free.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Where it sits](#where-it-sits)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
  - [DD1 — Resolve config in the CLI; pass the core options down](#dd1--resolve-config-in-the-cli-pass-the-core-options-down)
  - [DD2 — Discover `dialogue.toml` by walking up from the script](#dd2--discover-dialoguetoml-by-walking-up-from-the-script)
  - [DD3 — A compiler factory seam keeps `compile` config-aware and testable](#dd3--a-compiler-factory-seam-keeps-compile-config-aware-and-testable)
  - [DD4 — Thread options into visualize through a configured visualizer](#dd4--thread-options-into-visualize-through-a-configured-visualizer)
  - [DD5 — Autocompletion needs no new plumbing](#dd5--autocompletion-needs-no-new-plumbing)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)

## Goal and scope

The [Configuration](./Configuration.md) component made the compiler configurable and
the [configuration loader](./Configuration%20Loader.md) reads a `dialogue.toml` into a
`CompilerOptions`, but **nothing wires them into the CLI**: `dialoguedown compile` and
`dialoguedown visualize` always build the compiler with `CompilerOptions.Default`. This
component closes that last gap — a project's `dialogue.toml` reaches the compiler behind
both commands, so configured speakers appear in a compile, in the visualization report,
and in the report editor's **autocompletion**.

**In scope:** a `--config` option on both commands, automatic discovery of a
`dialogue.toml`, threading the resolved `CompilerOptions` into the `compile` compiler and
the `visualize` report (static export, `--emit`, and the served/live session), and a
user-guide page documenting it. **Out of scope:** other config knobs (deferred in the
Configuration note), and any change to the `dialogue.toml` schema or the loader.

## Where it sits

The CLI is the composition root that already wires the loader's inputs and the
visualizer. It gains one new step — **resolve the options** — between reading the
command line and building the compiler.

```mermaid
flowchart LR
    ARGS["--config / script"] --> R["ProjectConfiguration<br/>(CLI resolver)"]
    TOML["dialogue.toml"] -.discovered.-> R
    R -->|CompilerOptions| CMP["compile:<br/>IScriptCompiler"]
    R -->|CompilerOptions| VIZ["visualize:<br/>CompilationVisualizer"]
    VIZ --> SYM["report symbols → autocompletion"]
    style R fill:#2d6,stroke:#0a0,color:#000
```

The resolver is the only new type that depends on `DialogueDown.ConfigurationLoader`; it
hands the rest of the CLI a plain `CompilerOptions` (a core type), so the visualization
assemblies never take a TOML dependency.

## Ubiquitous language

| Term                      | Meaning                                                                                                                      |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Project configuration** | The `dialogue.toml` for a script's project, read into a `CompilerOptions`.                                                   |
| **Config resolution**     | Choosing which `CompilerOptions` a command uses: an explicit `--config`, an auto-discovered `dialogue.toml`, or the default. |
| **Configured visualizer** | A `CompilationVisualizer` built over a compiler configured with a project's options.                                         |

## Functionality checklist

- [ ] Both `compile` and `visualize` accept `--config <path>` naming a `dialogue.toml`.
- [ ] With no `--config`, the CLI discovers the nearest `dialogue.toml` by walking up from
      the script's directory (bounded by `--root` for `visualize`); absent, it uses
      `CompilerOptions.Default`.
- [ ] An explicit `--config` path that is missing or malformed fails with a clear usage
      error; a malformed discovered file surfaces the loader's located error.
- [ ] `compile` builds its compiler from the resolved options.
- [ ] `visualize` — static export, `--emit`, and the served/live session — builds its
      report from the resolved options.
- [ ] Configured speakers appear in the report's completion symbols, so the editor's
      speaker autocompletion offers them.
- [ ] The user guide documents `dialogue.toml` and `--config`.

## Interfaces and abstractions

| Type                                     | Visibility        | Responsibility                                                                               | Collaborators                                |
| ---------------------------------------- | ----------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `ProjectConfiguration`                   | internal (CLI)    | Resolve `CompilerOptions` from `--config` or a discovered `dialogue.toml`                    | `TomlConfigurationLoader`, `CompilerOptions` |
| `Func<CompilerOptions, IScriptCompiler>` | internal (CLI)    | The compile command's compiler factory seam (default: `ScriptCompilerFactory.CreateDefault`) | `CompileCommand`                             |
| `CompilationVisualizer(CompilerOptions)` | public (new ctor) | Build a visualizer over a configured compiler                                                | `ScriptCompilerFactory`                      |
| `IVisualizeRunner` (extended)            | public            | Carry `CompilerOptions` into each run mode                                                   | `CompilationVisualizer`, `LiveSession`       |
| `CompileSettings` / `VisualizeSettings`  | internal (CLI)    | Add the `--config` option                                                                    | Spectre.Console.Cli                          |

## Key design decisions

### DD1 — Resolve config in the CLI; pass the core options down

Only the CLI knows the command line and the working directory, so config **resolution**
(discovery + loading) lives there, in a small `ProjectConfiguration` type that references
`DialogueDown.ConfigurationLoader`. Everything downstream — the compiler factory, the
visualizer, the live session — receives a plain `CompilerOptions`, which is a core type
they already depend on. This keeps the TOML dependency at the outermost layer and leaves
the engine-agnostic core and the visualization assemblies unaware of the file format, the
same boundary the loader's architecture test guards.

### DD2 — Discover `dialogue.toml` by walking up from the script

Zero-config is the common case, so the CLI **discovers** a `dialogue.toml` rather than
requiring a flag — following the convention every established tool uses. `tsc`,
clang-format, Prettier, Black/Ruff, and EditorConfig all search from the input **upward**
to the nearest config. The CLI walks **up from the script's directory** to the first
`dialogue.toml` (nearest wins), so one config at a project root serves scripts nested in
subfolders — the normal project layout. An explicit `--config <path>` overrides discovery
and is the escape hatch for a config that lives elsewhere. For `visualize`, the walk is
**bounded by `--root`** when set: the served root is the security boundary, so discovery
never reads a config above what the user chose to serve. Precedence: **`--config` › nearest
`dialogue.toml` (walking up) › `CompilerOptions.Default`.** A `dialogue.toml` is itself the
project marker, so there is no separate EditorConfig-style `root = true` stop.

### DD3 — A compiler factory seam keeps `compile` config-aware and testable

`CompileCommand` currently takes an injected `IScriptCompiler` singleton built at startup
with default options — too early to know the resolved config. It instead takes a
**compiler factory** (`Func<CompilerOptions, IScriptCompiler>`, default
`ScriptCompilerFactory.CreateDefault`) and builds the compiler *after* resolving options.
A test still substitutes the factory to return a mock compiler and asserts both the
compile call and that the resolved options flowed through — preserving the current
mock-based command test.

### DD4 — Thread options into visualize through a configured visualizer

The visualize modes each construct `new CompilationVisualizer()` (default compiler). A new
public `CompilationVisualizer(CompilerOptions)` ctor builds the visualizer over
`ScriptCompilerFactory.CreateDefault(options)`, and the `IVisualizeRunner` methods gain a
`CompilerOptions` parameter that the command supplies and each mode forwards — static
export, `--emit`, and the served session (through `LiveSession`'s existing
`CompilationVisualizer?` seam). One options value flows to every visualize path.

### DD5 — Autocompletion needs no new plumbing

The report's editor completions come from `SymbolProjection.Project(SemanticModel)`, which
reads speakers, `@id`s, and tags from the **compiled semantic model's speaker table** —
not from re-scanning the script text. Configured speakers are already bound into that table
by the semantic analyzer, so once the visualizer compiles with the resolved options, they
flow `CompilerOptions → SemanticModel.Speakers → SymbolProjection → report symbols → editor
completions` with no change to the projection or the web client. This component only proves
it with a test.

## Error and boundary cases

| Case                                                               | Behavior                                                                       |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------ |
| No `--config`, no `dialogue.toml` found                            | `CompilerOptions.Default` (unchanged behavior).                                |
| `--config` path does not exist                                     | Usage error naming the missing file; no compile.                               |
| Discovered or `--config` file is malformed                         | The loader's located `DialogueConfigurationException` surfaces as a CLI error. |
| `dialogue.toml` beside the script and a different `--config` given | `--config` wins.                                                               |
| `visualize` launcher with no script                                | Discover by walking up from the browse root; else default.                     |
| Nearest `dialogue.toml` sits above `--root` in `visualize`         | Not discovered — the walk stops at `--root`; use `--config` to point at it.    |
| Empty / speaker-less `dialogue.toml`                               | `CompilerOptions.Default` (loader's behavior); nothing configured.             |

## Integration

- **CLI** (`DialogueDown.Cli`): references `DialogueDown.ConfigurationLoader`; adds
  `ProjectConfiguration`, the `--config` option, and the compiler-factory registration.
- **Visualization** (`DialogueDown.Visualization`): adds the public
  `CompilationVisualizer(CompilerOptions)` ctor.
- **Live** (`DialogueDown.Visualization.Live`): `IVisualizeRunner` and its modes take a
  `CompilerOptions`; the served session builds a configured `LiveSession`.
- **Docs**: a new `docs/guide/configuration.md` page (registered in the guide `toc.yml`),
  and a cross-link from the script-language guide.
- **Architecture**: unchanged boundaries — the CLI already sits outside the core; only the
  CLI gains the loader reference.

## Testability

- **`ProjectConfiguration`**: resolves an explicit `--config`, a discovered `dialogue.toml`,
  and the default; a missing explicit path errors; a malformed file surfaces the located
  error. Driven with a temp directory and raw-string TOML.
- **`compile`**: substitutes the compiler factory, asserts the resolved options reach it and
  the source is compiled; a `--config` file changes the options passed.
- **`visualize`**: the runner receives the resolved options for static, emit, and served
  routes (extending the existing routing tests).
- **Autocompletion**: a visualizer built from options carrying a configured speaker emits
  that speaker in the report's `SymbolSet`.
- **Docs**: markdownlint and link checks over the new guide page.
