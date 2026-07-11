<p align="center">
  <img src="assets/logo.svg" alt="DialogueDown logo" width="120" height="120" />
</p>

# DialogueDown

Engine-agnostic, C#-first branching **dialogue library**. Kept free of any Godot
dependency so the core (dialogue graph, runner, effects, conditions) is
**reusable across projects** and **unit-testable** in isolation. Engine-specific
presentation (panels, typewriter, input) lives in each consuming game as a thin
adapter over this library's interfaces.

> [!NOTE]
> DialogueDown is a work-in-progress open-source project. The public API,
> script language, and runtime model may change while the library is still in
> early development.

## Table of contents

- [Status](#status)
- [Layout](#layout)
- [Build and test](#build-and-test)
- [Documentation](#documentation)
- [Compilation visualization](#compilation-visualization)
- [Design intent](#design-intent)
- [Similar projects](#similar-projects)
- [Contributing](#contributing)
- [Security](#security)
- [License](#license)

## Status

- **Maturity:** early development.
- **Target framework:** .NET 8 (`net8.0`).
- **Engine dependency:** none in the core library.
- **Primary consumer:** Godot/C# game projects through `ProjectReference`.

## Layout

| Path | Purpose |
| --- | --- |
| `src/DialogueDown/` | the reusable class library (net8.0, no engine refs) |
| `src/DialogueDown.Visualization/` | diagnostics-only visualizer of compiler stages (not shipped in the core package) |
| `src/DialogueDown.Visualization.Live/` | loopback server that serves the report, hot-reloads it on edit, and hosts the launcher |
| `src/DialogueDown.Cli/` | the `dialoguedown` command-line interface (`compile`, `visualize`) |
| `tests/DialogueDown.Tests/` | xUnit tests for the pure logic |
| `tests/DialogueDown.Visualization.Tests/` | xUnit tests for the visualizer |
| `tests/DialogueDown.Visualization.Live.Tests/` | xUnit tests for the live server and launcher |
| `tests/DialogueDown.Cli.Tests/` | xUnit tests for the CLI |

## Build and test

Restore, build, and test the solution:

```bash
dotnet restore DialogueDown.sln
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln
```

To collect source-focused coverage for the core library:

```bash
dotnet tool restore
dotnet test DialogueDown.sln \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage"
dotnet reportgenerator \
  "-reports:tests/**/TestResults/**/coverage.cobertura.xml" \
  "-targetdir:coverage-report" \
  "-reporttypes:Html;MarkdownSummary;Cobertura"
```

Coverage is verified against the `DialogueDown` and `DialogueDown.Visualization`
source assemblies and excludes test files. The collector writes Cobertura XML under
`TestResults/`, and ReportGenerator writes an interactive HTML report to
`coverage-report/index.html`. Both output folders are ignored by Git.

CI fails if line coverage drops below 90% and emits a warning when it is below
100%.

## Documentation

📖 **[Documentation site](https://pengzhengyi.github.io/godot-dialoguedown/)** — the
writer guide, the contributing docs and per-stage design notes, and the generated
C# API reference, published from `docs/` on every merge to `main`.

In the repository:

- [Overview](docs/guide/overview.md), architecture, representations, and current
  implementation status.
- [Script language specification](docs/guide/script-language.md)
  for proposed writer-facing dialogue syntax.
- [Design notes](docs/contributing/design-notes/README.md) — the goal, key
  decisions, and tradeoffs behind each compiler stage and component.

## Compilation visualization

<p align="center">
  <img src="assets/logo-pipeline.svg" alt="A choice node branching to two options that each lead to a scene" width="132" height="132" />
</p>

> [!TIP]
> **[▶ Try the live demo](https://pengzhengyi.github.io/godot-dialoguedown/demo/)** — an
> interactive, read-only report for a sample script, served from GitHub Pages and
> rebuilt on every merge to `main`.

DialogueDown aims to be **transparent end to end**: you can *see* what the
compiler produced at each stage. The optional
[`DialogueDown.Visualization`](src/DialogueDown.Visualization/) project renders the
compiler's stages as an interactive HTML report — a **Source** tab (the whole
document beside a live preview, with working anchor links), then a graph tab for
each stage: **Markdown AST**, **Dialogue AST**, and the desugarer's **Desugared
AST**, each with pan and zoom and click-to-collapse. Graphs remember where you
left them — an adjusted graph keeps its own view while an untouched one inherits
the current one — and open on a readable, root-centered default; the zoom toolbar
takes a typed percentage or a one-click revert. Any tab can go **full screen** — a
maximize button (or the <kbd>f</kbd> key) fills the window with the active graph or
source split and hides the header, tabs, and status bar; <kbd>f</kbd> or <kbd>Esc</kbd>
restores it. A served report toggles
between **View** (read-only, auto-updating) and **Edit** (an in-browser editor that
saves back to the file). Click a node to inspect **the source it was produced
from**, with a rendered Markdown preview, in a resizable side panel. Nodes are
**color-coded by a cross-stage category** (a code span and the game call it
becomes share a color), with a legend and arrow-key navigation. The report is a
**single self-contained HTML file** — D3, CodeMirror, Pico.css, marked, and
Tippy.js are all bundled in, so it needs no server and works fully offline. It
reads the compiler through the same seams the tests use and never touches the
shipped core package, so the core stays dependency-light.

Render a script from the command line with the `dialoguedown visualize` command:

```bash
# Open the launcher to browse for a script (the uniform entry point)
dotnet run --project src/DialogueDown.Cli -- visualize

# Serve a script's report and toggle View ⇄ Edit in the browser (auto-updates on save)
dotnet run --project src/DialogueDown.Cli -- visualize scene.dialogue.md --root .

# Start directly in Edit (editable, saves back to the file)
dotnet run --project src/DialogueDown.Cli -- visualize scene.dialogue.md --edit --root .

# Export a self-contained report to a file (no server, no browser)
dotnet run --project src/DialogueDown.Cli -- visualize scene.dialogue.md -o report.html

# Emit each stage's graph as portable Mermaid or Graphviz DOT text (to stdout or -o)
dotnet run --project src/DialogueDown.Cli -- visualize scene.dialogue.md --emit mermaid
dotnet run --project src/DialogueDown.Cli -- visualize scene.dialogue.md --emit dot -o scene.dot
```

> [!NOTE]
> The visualizer is a diagnostics helper, built quickly with lighter review than
> the core library; its API and abstractions may still change.

See the
[Compilation Visualization note](docs/contributing/design-notes/Compilation%20Visualization.md).

## Design intent

- **Data:** dialogue graph as nodes + choices (id-referenced edges).
- **Logic:** an `IDialogueRunner` humble-object drives current-node / choices /
  effects and is fully unit-testable.
- **Effects & conditions:** Command / predicate objects, so new outcomes are new
  types, not edits to the runner (Open/Closed).
- **Presentation:** lives in the consuming engine, behind the library's
  interfaces. Swap roll-your-own today for Ink/Dialogue Manager later without
  touching game code.

## Consumers

Referenced by games via `ProjectReference`, for example `survival-game-learner`.

## Similar projects

DialogueDown is intentionally small, engine-agnostic, and C#-first. These
projects are useful references if you need a different tradeoff:

| Project | What it does | How DialogueDown differs |
| --- | --- | --- |
| [Ink](https://github.com/inkle/ink) | Mature interactive-fiction scripting language and runtime with strong authoring tools. | DialogueDown keeps Markdown-like source close to game writing notes and focuses on a lightweight C# library that Godot projects can reference directly. |
| [Yarn Spinner](https://github.com/YarnSpinnerTool/YarnSpinner) | Full-featured Yarn dialogue compiler/runtime with a writer-friendly scripting language and broad engine integrations. | DialogueDown is narrower and dependency-light: it prioritizes pure C# graph/runtime seams and explicit visualization over a larger cross-engine toolchain. |
| [Dialogic](https://github.com/coppolaemilio/dialogic) | Feature-rich Godot dialogue plugin with visual editing, portraits, timelines, variables, and localization. | DialogueDown deliberately avoids Godot dependencies in the core so dialogue logic stays reusable, unit-testable, and portable across consuming games. |
| [Godot Dialogue Manager](https://github.com/nathanhoad/godot_dialogue_manager) | Godot-native dialogue manager and scripting workflow for branching conversations. | DialogueDown targets engine-agnostic C# packages first, leaving Godot presentation and input as thin adapters in each game. |
| [Godot Ink](https://github.com/paulloz/godot-ink) | Godot integration for Ink stories. | DialogueDown is not an Ink bridge; it explores a smaller Markdown-to-dialogue pipeline with compiler-stage visualization for debugging and teaching. |

## Contributing

Contributions are welcome while the project is still taking shape. Start with
[CONTRIBUTING.md](CONTRIBUTING.md) for local setup, commit style, tests, and pull
request expectations.

Please follow the [Code of Conduct](CODE_OF_CONDUCT.md) in all project spaces.

## Security

Please don't report vulnerabilities in public issues. See
[SECURITY.md](SECURITY.md) for the current reporting process.

## License

DialogueDown is released under the [MIT License](LICENSE).
