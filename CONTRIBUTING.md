# Contributing to DialogueDown

Thank you for considering a contribution. DialogueDown is early stage, so
small, well-scoped changes are easiest to review and merge.

## Ways to contribute

- Report bugs with a minimal reproduction.
- Propose script-language or API improvements.
- Improve documentation and examples.
- Add tests for current behavior.
- Fix small, focused issues.

## Before you start

1. Search existing issues and pull requests to avoid duplicate work.
2. Open an issue for larger behavior, API, or syntax changes before writing code.
3. Keep pull requests small and focused.

## Development setup

Requirements:

- .NET SDK 8 or newer
- Git

Clone the repository and run:

```bash
dotnet restore DialogueDown.sln
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln --configuration Release --no-build
```

To collect coverage focused on production source code:

```bash
dotnet tool restore
dotnet test DialogueDown.sln \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage"
dotnet reportgenerator \
  "-reports:tests/DialogueDown.Tests/TestResults/**/coverage.cobertura.xml" \
  "-targetdir:coverage-report" \
  "-reporttypes:Html;MarkdownSummary;Cobertura"
```

Coverage is verified against the `DialogueDown` source assembly and excludes
test files. Cobertura output is written under `TestResults/`, and the interactive
report is written to `coverage-report/index.html`.

CI fails below 90% line coverage and warns below 100%.

## Commit style

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
docs: improve script language examples
fix(parser): handle empty dialogue files
test(tags): cover default speaker tag
```

Use one logical change per commit. Mark breaking API or script-language changes
with `BREAKING CHANGE:` in the commit footer.

## Pull request checklist

Before opening a pull request:

- [ ] Add or update tests for behavior changes.
- [ ] Update documentation for public API or script-language changes.
- [ ] Run `dotnet test DialogueDown.sln`.
- [ ] Run source-focused coverage when changing tested behavior.
- [ ] Keep the pull request focused on one topic.
- [ ] Explain why the change is useful.

## Code of conduct

All participants must follow the [Code of Conduct](CODE_OF_CONDUCT.md).
