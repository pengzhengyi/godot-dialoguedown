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
| `tests/DialogueDown.Tests/` | xUnit tests for the pure logic |
| `tests/DialogueDown.Visualization.Tests/` | xUnit tests for the visualizer |

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

- [Overview](docs/Overview.md), architecture, representations, and current
  implementation status.
- [Script language specification](docs/Script%20Language/Script%20Language%20DSL%20Specification.md)
  for proposed writer-facing dialogue syntax.

## Compilation visualization

DialogueDown aims to be **transparent end to end**: you can *see* what the
compiler produced at each stage. The optional
[`DialogueDown.Visualization`](src/DialogueDown.Visualization/) project renders a
stage's intermediate representation as an interactive HTML report — one tab per
stage, pan and zoom, click a node to collapse or expand — plus Mermaid and DOT
text for quick embedding. Click a node to inspect **the source it was produced
from**, with a rendered Markdown preview, in a side panel. The report loads D3,
Pico.css, and marked from a CDN and falls back to bundled copies, so it still
works offline. It reads the compiler through the same seams the tests use and
never touches the shipped core package, so the core stays dependency-light. The
Markdown AST view ships today; the Dialogue AST view lands with the transpiler.

> [!NOTE]
> The visualizer is a diagnostics helper, built quickly with lighter review than
> the core library; its API and abstractions may still change.

See the
[Compilation Visualization note](docs/Implementation%20Notes/Compilation%20Visualization.md).

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
