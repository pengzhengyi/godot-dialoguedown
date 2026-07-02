# DialogueSystem

Engine-agnostic, C#-first branching **dialogue library**. Kept free of any Godot
dependency so the core (dialogue graph, runner, effects, conditions) is
**reusable across projects** and **unit-testable** in isolation. Engine-specific
presentation (panels, typewriter, input) lives in each consuming game as a thin
adapter over this library's interfaces.

## Layout

| Path | Purpose |
| --- | --- |
| `src/DialogueSystem/` | the reusable class library (net8.0, no engine refs) |
| `tests/DialogueSystem.Tests/` | xUnit tests for the pure logic |

## Build & test

```bash
dotnet test DialogueSystem.sln
```

## Documentation

- [Overview](docs/Overview.md) — architecture, representations, and current
  implementation status.
- [Script language specification](docs/Script%20Language/Script%20Language%20DSL%20Specification.md)
  — proposed writer-facing dialogue syntax.

## Design intent

- **Data:** dialogue graph as nodes + choices (id-referenced edges).
- **Logic:** an `IDialogueRunner` humble-object drives current-node / choices /
  effects and is fully unit-testable.
- **Effects & conditions:** Command / predicate objects so new outcomes are new
  types, not edits to the runner (Open/Closed).
- **Presentation:** lives in the consuming engine, behind the library's
  interfaces — swap roll-your-own today for Ink/Dialogue Manager later without
  touching game code.

## Consumers

Referenced by games via `ProjectReference` (e.g. `survival-game-learner`).
