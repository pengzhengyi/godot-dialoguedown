---
applyTo: "**/*.cs"
---

# C# conventions

Apply to the library, CLI, and tests. Full setup and the pull-request checklist
are in [`CONTRIBUTING.md`](../../CONTRIBUTING.md); the error model and per-stage
design live in [`docs/contributing/design-notes/`](../../docs/contributing/design-notes/README.md).

## Style

- The build enforces **StyleCop.Analyzers** and `.editorconfig`; follow what they
  flag rather than fighting them.
- **File-scoped namespaces.** `using` directives sit **outside** the namespace,
  system directives first; unused usings are a warning (IDE0005).
- Prefer **`var`** for built-in and apparent types, **expression-bodied members**
  when they fit on one line, and **pattern matching** over `is`/`as` with casts.
- Use **LINQ** when it is a clear readability win over an explicit loop; keep it
  simple, not clever.
- Four-space indent, LF line endings, UTF-8.
- Name for intent; keep methods small. Comment the *why*, not the *what*.

## Design and errors

- Follow **SOLID** and **composition over inheritance**; reach for a pattern only
  when it removes real duplication or coupling.
- Throw **specific, meaningful exceptions** that name the offending input and the
  expectation. Extract non-trivial validation into a small, well-named helper.
- Use the project **error model** (exception hierarchy, spans, message
  conventions) documented in the
  [design-notes README](../../docs/contributing/design-notes/README.md); component
  notes link back to it instead of redefining errors.

## Tests

- **Test-driven:** a failing test first, then the minimum code, then refactor. A
  bug fix starts with a test that reproduces it.
- **xUnit** with **NSubstitute** for mocks and **coverlet** for coverage.
- **One test file per source file**, mirroring the folder layout and named for the
  type under test (`Foo.cs` → `FooTests.cs`).
- Name tests `Method_Scenario_ExpectedResult`; cover edge and error cases, not just
  the happy path.
- Write multi-line test input as **multi-line raw string literals** (`"""…"""`),
  not single lines stitched with `\n`, so the parsed shape is visible.
- Build a type's dependencies through the shared **test factory** (Object Mother)
  so a constructor change touches one place.
- **Treat tests as code:** refactor them relentlessly, and follow the testing
  pyramid — many small unit tests, fewer integration tests, minimal end-to-end.
- **Guard architecture with tests.** Dependency-direction rules live in
  [`tests/DialogueDown.Architecture.Tests`](../../tests/DialogueDown.Architecture.Tests),
  built on **NetArchTest.eNhancedEdition** (the maintained fork of NetArchTest's
  simple fluent API) rather than hand-rolled reflection. They assert the assembly
  boundaries (core `DialogueDown` must not depend on `DialogueDown.Cli`, the
  `DialogueDown.Visualization*` projects, or any Spectre/Godot/console package) and
  the core's internal layering (the Dialogue AST stays decoupled from Markdown; the
  pipeline never calls back into the compilation orchestrator). Extend that suite
  when you add a layer or boundary.

## Before pushing

```bash
dotnet test DialogueDown.sln --configuration Release
```

Run source-focused coverage when you change tested behavior (see
[`CONTRIBUTING.md`](../../CONTRIBUTING.md)).
