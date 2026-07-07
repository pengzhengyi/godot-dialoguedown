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

### Visualization frontend (`web/`)

The compilation report's client is a self-contained TypeScript + Vite project in
`src/DialogueDown.Visualization/web/`. The .NET library embeds its **built**
single-file report (`web/dist/report.html`), which is committed to the repo, so a
plain `dotnet build` needs no Node. You only need Node (20+) to change the client:

```bash
cd src/DialogueDown.Visualization/web
npm install
npm run dev                        # live-reloading dev server with sample data
npm run check                      # typecheck, lint, style, format, unit tests
npx playwright install chromium    # once, for e2e
npm run e2e                        # Playwright end-to-end + accessibility tests
npm run build                      # rebuild the committed dist/report.html
```

If you change anything under `web/src`, **rebuild and commit
`web/dist/report.html`** — CI fails if the committed report is out of sync with its
sources.

### Editor tasks (VS Code)

Common tasks are wired up in `.vscode/tasks.json` (**Terminal → Run Task**), so
you can build, test, and clean without memorising commands: `build` / `test`
(.NET), `web: build` / `web: check` / `web: e2e` (frontend), `build: all` and
`verify: all` (both stacks), and `clean` (remove build/test artifacts).

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
- [ ] If you changed the visualization frontend (`web/`), rebuild and commit `web/dist/report.html`.
- [ ] Keep the pull request focused on one topic.
- [ ] Explain why the change is useful.

## Code of conduct

All participants must follow the [Code of Conduct](CODE_OF_CONDUCT.md).
