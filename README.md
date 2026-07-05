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
| `tests/DialogueDown.Tests/` | xUnit tests for the pure logic |

## Build and test

Restore, build, and test the solution:

```bash
dotnet restore DialogueDown.sln
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln
```

## Documentation

- [Overview](docs/Overview.md), architecture, representations, and current
  implementation status.
- [Script language specification](docs/Script%20Language/Script%20Language%20DSL%20Specification.md)
  for proposed writer-facing dialogue syntax.

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
