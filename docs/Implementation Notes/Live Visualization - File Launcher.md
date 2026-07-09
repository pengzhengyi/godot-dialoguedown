# Live Visualization — File Launcher

> [!NOTE]
> Status: **proposed** (Component 3 of live visualization; not yet implemented).
> Component 1 (Hot Reload) shipped a `visualize` command that serves one document
> from a loopback server and hot-reloads it. This component makes a **launcher** the
> uniform interactive entry point: `visualize` opens a browser modal — like VS Code's
> Open File — to choose a **root folder**, a `.dialogue.md` **source** under it, and a
> **mode**, then opens the report. CLI arguments pre-fill the modal; a fully specified
> command skips it. **Live editing** (Component 2) is a separate, later component, so
> its mode option is shown but disabled here.
>
> Unlike the core library, this component is part of the "vibe-coded" visualization
> tooling (see the visualization note's maturity caveat); the core engine remains the
> carefully-reviewed surface.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Architecture](#architecture)
- [CLI entry point](#cli-entry-point)
- [Launch flow](#launch-flow)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [HTTP surface](#http-surface)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Security](#security)
- [Integration](#integration)
- [Testability](#testability)

## Goal and scope

Today you visualize one script per CLI invocation: `visualize scene.dialogue.md`. To
look at another script you stop the server and re-run the command, and there is no way
to discover scripts from the browser. The launcher makes visualization start from a
single, uniform place — a modal, like VS Code's Open File — that lets you choose a root
folder, browse the `.dialogue.md` scripts under it (however deeply nested), pick a mode,
and open the report. When you already know exactly what you want, the CLI arguments
pre-fill or bypass the modal entirely.

In scope:

- The launcher as the uniform **interactive** entry point for `visualize`.
- A modal to choose a **root folder** (default: the current directory), a **source**
  file that is a descendant of the root, and a **mode**.
- CLI arguments **pre-fill** the modal; a command that specifies the source, mode, and
  root **bypasses** it and opens the report directly. `--pick` always shows the modal.
- Static and hot-reload modes, reusing Component 1. The **Live Edit** mode is shown but
  disabled until Component 2.
- A way back to the launcher from a report.

Out of scope (deferred to later components):

- **Live editing** (in-browser editor, Save, dirty tracking) — Component 2. Its option
  appears disabled here.
- The non-interactive **`-o` export** (write a self-contained file, no browser) keeps
  its current direct behavior; the launcher is the interactive path only.
- Serving scripts from **multiple roots at once**, or a persistent recent-files history.
- A native OS file dialog (see [D2](#d2--a-server-side-browser-not-a-native-file-dialog)).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Launcher** | The modal (and the server behind it) that is the uniform interactive entry point. |
| **Launch root** | The single directory subtree the launcher may browse and serve — the security boundary. Chosen in the modal (default: the current directory) or with `--root`. |
| **Source** | A `.dialogue.md` file, a descendant of the launch root, selected to open. |
| **Mode** | How the source is opened: **Static** (served once), **Watch** (hot-reload, Component 1), or **Live Edit** (Component 2, shown disabled). |
| **Open** | Confirming a root, source, and mode so the report is rendered and shown. |
| **Pick** | Forcing the modal to appear even when the command is fully specified (`--pick`). |
| **Session** | (Existing.) A rendered document the server serves; the launcher creates one per open. |

## Functionality checklist

- [ ] `visualize` (no arguments) opens the launcher modal rooted at the current
      directory, with `.dialogue.md` candidates highlighted.
- [ ] The modal chooses a **root folder**, a **source** under it (browsable to any
      depth, never above the root), and a **mode** (Static / Watch / Live Edit —
      the last disabled).
- [ ] CLI arguments **pre-fill** the modal: `visualize sub/scene.dialogue.md --watch`
      opens the modal with that source and Watch selected.
- [ ] A command that specifies **source, mode, and root** opens the report directly
      (no modal); `--pick` forces the modal even then.
- [ ] Opening renders the report and navigates the browser to it; the report behaves
      as today (tabs, mode badge, hot reload when watched).
- [ ] The report offers a **Back to launcher** affordance when opened via the launcher.
- [ ] Opening another source from the launcher replaces the active one.
- [ ] Requests for paths outside the launch root, or for non-`.dialogue.md` sources,
      are rejected.
- [ ] The non-interactive `-o` export path is unchanged.

## Architecture

`visualize` resolves its arguments into an initial selection (root, source, mode), then
either opens the report directly (fully specified, no `--pick`) or starts the launcher
server and opens the modal pre-filled with that selection. The launcher reuses
Component 1's server and session; it adds the modal page, two small APIs, and the
argument-to-selection mapping.

```text
visualize [args] ──► resolve selection (root, source?, mode)
        │
        ├── source + mode + root all set, no --pick ──► open report directly (Component 1)
        │
        └── otherwise ──► LauncherMode starts LiveVisualizationServer, opens the modal:
                              GET  /              → launcher modal (pre-filled selection)
                              GET  /api/browse    → folders + .dialogue.md entries (JSON)
                              POST /api/open      → set active session, 303 → report
                              GET  /<report>, /api/document, /api/events   (existing)
                              static files served under the launch root
```

- **Active launch root.** At any time the server serves static files under one launch
  root — the currently selected one. Choosing a root in the modal (or `--root`) sets it;
  the static-file provider follows the active root, so a source's relative assets resolve
  in place. Binding serving to the root removes Component 1's per-document serve-root
  negotiation: choosing a root *is* the hosting consent for that subtree (see
  [Security](#security)).
- **One active session, swapped on open.** The server keeps at most one live session and
  swaps it when you open another source. This reuses the existing single-session server
  rather than generalizing it to many concurrent documents (a possible later extension).
- **Two-page bundles.** The report stays a self-contained single file
  (`dist/report.html`). The launcher is a **second** inlined bundle
  (`dist/launcher.html`), embedded in the assembly the same way, served at `/` with its
  initial selection injected like the report's data.

## CLI entry point

```text
visualize [<source>] [--root <dir>] [--mode static|watch|live] [--pick]
          [-o <file>] [--port <n>] [--no-open]
```

The command resolves an initial **selection** from its arguments, then decides how to
proceed:

| Argument | Effect on the selection |
| --- | --- |
| `<source>` (optional `.dialogue.md`) | Pre-selects the source. The root defaults to `--root` if given, else the current directory when the source is under it, else the source's own directory. |
| `--root <dir>` | Sets the launch root; the source must be a descendant. |
| `--mode static\|watch\|live` | Sets the mode. `--watch` and `--live` are shorthands for `--mode watch` / `--mode live`. `live` is accepted but not yet functional (Component 2). |
| `--pick` | Always show the modal, even when the selection is complete. |
| `-o <file>`, `--port`, `--no-open` | As today. `-o` is the non-interactive export and never opens the modal. |

**Bypass rule.** When `<source>`, `--mode`, and `--root` are all explicitly given and
`--pick` is absent, `visualize` opens the report directly (Component 1's static or watch
path). Otherwise, it starts the launcher and opens the modal pre-filled with whatever the
arguments did specify. So `visualize` alone opens an empty modal at the current
directory, `visualize scene.dialogue.md --watch` opens the modal pre-filled (root not
given), and `visualize scene.dialogue.md --mode watch --root .` opens the report
directly.

## Launch flow

1. `visualize [args]` resolves a selection. If it is complete and `--pick` is absent,
   the report opens directly (Component 1) and the steps below are skipped.
2. Otherwise, `LauncherMode` starts the server and opens `/`; the modal loads pre-filled
   with the resolved selection (root, source if any, mode).
3. The modal requests `GET /api/browse?path=` for the active root and renders its
   directories and `.dialogue.md` entries; descending or changing the root re-requests
   `browse`, which confines every path to (or re-roots at) an allowed directory.
4. The user confirms a root, a source, and a mode, then clicks Open →
   `POST /api/open { root, source, mode }`.
5. The server validates the paths, sets the active root, (re)creates the active session
   for the source, starts the watcher when the mode is watch, and responds
   `303 See Other` with the report URL.
6. The browser follows the redirect to the report, which behaves as today. When watched,
   edits on disk hot-reload it (Component 1). **Back to launcher** navigates to `/`.

## Interfaces and abstractions

- **`ILauncherRunner` / `LauncherRunner`** (new, `DialogueDown.Visualization.Live`):
  `Task<int> RunAsync(LaunchSelection initial, int? port, bool noOpen, CancellationToken)`,
  where `LaunchSelection` is the resolved `{ root, source?, mode }`. Started by the CLI
  whenever the report is not opened directly. Parallels `WatchMode`.
- **`LaunchRoot`** (new): a validated root plus path helpers (`TryResolveEntry`,
  `Browse`) that confine every path to the active root; the active root can be replaced
  when the user chooses a new one.
- The report/session path reuses `LiveSession`, `ServeRoot`, and `DocumentWatcher`; the
  CLI keeps using `IVisualizeRunner` for the direct (bypass) path and adds
  `ILauncherRunner` for the modal path.
- **Frontend**: a new `launcher.ts` entry driving the browse tree, the root/source/mode
  selection, and the open request; it shares styling and helpers (path display,
  tooltips) with the report.

## HTTP surface

| Method + path | Purpose | Response |
| --- | --- | --- |
| `GET /` | The launcher modal, with its initial selection injected (like the report's data) | `text/html` (embedded `launcher.html`) |
| `GET /api/browse?path=<dir>` | List a directory's sub-directories and `.dialogue.md` entries | JSON `{ path, parent, directories[], entries[] }` |
| `POST /api/open` | Open a selection (`{ root, source, mode }`) | `303 See Other` → report URL (or `400`/`404`) |
| `GET /<report path>` | The active session's report | `text/html` (existing) |
| `GET /api/document`, `GET /api/events` | The active session's data and SSE | Existing (Component 1) |

## Key design decisions

### D1 — The launcher is the uniform interactive entry point

`<source>` becomes optional on `visualize`, and the launcher — not a direct render — is
the default interactive entry. The command resolves a selection from its arguments and
opens the modal pre-filled with it. The one fast path is the **bypass rule**: when
`<source>`, `--mode`, and `--root` are all explicit and `--pick` is absent, the report
opens directly (Component 1). This keeps one obvious entry point (no sub-command) while
preserving a no-modal path for fully specified commands and `--pick` to force the modal.

### D2 — A server-side browser, not a native file dialog

A browser `<input type="file">` yields the file's *contents and name* but never its
absolute path. The tool needs the path to watch the file and resolve its relative
images, so a native dialog cannot drive it. The launcher instead browses the server's
filesystem through `GET /api/browse`, which also lets the server enforce the launch
root and the `.dialogue.md` filter.

### D3 — Multi-page navigation, not a client SPA

The report is a self-contained single file with embedded data; embedding a router in
it to host a launcher view would bloat it and entangle the two. The launcher is a
separate page and opening an entry is a full navigation (a `303` to the report). No
client-side router is introduced.

### D4 — The launcher is a second inlined bundle

`launcher.html` is a second Vite entry, inlined to a single file by the existing
single-file plugin and embedded as an assembly resource like `report.html`. This keeps
the TypeScript, styling, and Node-free .NET build consistent, at the cost of a second
build output and embedded resource.

### D5 — The active launch root is the hosting boundary

The launcher serves exactly one subtree at a time — the active launch root — and every
`browse`/`open` path is confined to it. The root is chosen in the modal (default: the
current directory) or with `--root`, and can be changed before opening, like VS Code's
Open Folder. Choosing a root is the hosting consent for that subtree, replacing
Component 1's per-document negotiation. Selecting a *new* root is a broad,
filesystem-level pick (inherent to any folder chooser), but the server is loopback-only
and only the chosen root's files are ever served (see [Security](#security)).

### D6 — One active session, swapped on open

The server keeps at most one live session and swaps it when you open another source.
This reuses the existing single-session, single-serve-root server unchanged rather than
generalizing it to many concurrent documents (a possible later extension).

### D7 — A mode option group; live editing is deferred

The modal presents a **Static / Watch / Live Edit** option group (matching the CLI
`--mode`). Static serves once, Watch hot-reloads (Component 1). **Live Edit** is shown
but **disabled** ("coming soon") until Component 2 lands, so the launcher already
reflects the full mode set without offering a non-functional option.

### D8 — "Back to launcher" only when launched

The report shows a **Back to launcher** control only when it is served by a launcher
server (a flag in the injected report data), so a bypassed
`visualize scene.dialogue.md --mode watch --root .` report is unchanged.

### D9 — The `-o` export stays a non-interactive path

`-o <file>` writes a self-contained report to disk with no server and no browser, so it
cannot go through the modal. It keeps Component 1's behavior and is never affected by
the launcher; the launcher is the *interactive* entry point only.

## Error and boundary cases

- **Path escapes the root** (`..`, absolute, or a symlink pointing outside): `browse`
  and `open` reject it with `400`/`404`; nothing outside the root is listed or served.
- **Not a `.dialogue.md` file**, or the file was deleted between listing and open:
  `open` returns `404`; the page surfaces a message and refreshes the listing.
- **Empty directory / no entries**: the page shows an explicit empty state.
- **A watched, opened file is deleted**: handled by Component 1 (the report shows its
  deletion banner); **Back to launcher** still works.
- **Invalid `--root`** (missing, not a directory): the CLI fails fast with exit code
  `64` (usage), consistent with the existing argument validation.

## Security

The launcher exposes a filesystem-reading API on a loopback server, so path confinement
is the central concern.

- **Loopback only.** The server binds `127.0.0.1`, as Component 1 does.
- **Root confinement.** Every `browse`/`open` path *within a chosen root* is resolved
  against the active root and canonicalized (resolving `..` and symlinks); a result whose
  real path is not inside the active root is rejected. Absolute paths and traversal
  within a root are refused, and only the active root's files are ever served.
- **Extension filter.** Only directories and `.dialogue.md` files are listed or opened.
- **Deliberate root choice.** The root defaults to the current directory (or `--root`).
  Changing it in the modal is a broad, filesystem-level pick — inherent to any folder
  chooser (VS Code's Open Folder is the same) — but it is an explicit user action on
  their own machine, the server is loopback-only, and serving is always scoped to the
  chosen root. The launcher never widens serving on its own.

## Integration

- **CLI** (`DialogueDown.Cli`): `VisualizeSettings` makes `<source>` optional and adds
  `--root`, `--mode` (with `--watch`/`--live` shorthands), and `--pick`;
  `VisualizeCommand` applies the bypass rule — direct render when source, mode, and root
  are all set and `--pick` is absent, otherwise the launcher. `CliServices` registers the
  launcher runner.
- **Live** (`DialogueDown.Visualization.Live`): adds `LauncherMode`/`LauncherRunner`,
  `LaunchRoot`, and the `browse`/`open` endpoints on `LiveVisualizationServer`; reuses
  `LiveSession`, `ServeRoot`, `DocumentWatcher`, and the SSE broadcaster.
- **Frontend** (`web/`): adds the `launcher.html` entry and `launcher.ts`; the build
  emits a second inlined bundle; the report gains an optional **Back to launcher**
  control. Both bundles stay committed and CI-synced.

## Testability

- **Path confinement** (unit): `LaunchRoot` rejects `..`, absolute paths, and symlink
  escapes; accepts entries inside the root. Highest-value security tests.
- **Browse/open API** (integration): start `LiveVisualizationServer` in launcher mode
  over a temp tree; assert `browse` lists only directories and `.dialogue.md` entries,
  `open` returns `303` to the report and swaps the active session, and out-of-root or
  wrong-extension requests are refused.
- **CLI** (unit): the bypass rule — a complete `source + --mode + --root` (without
  `--pick`) invokes the direct runner, while any missing piece or `--pick` invokes the
  launcher runner (both substituted); argument-to-selection mapping and `--root`
  validation.
- **Frontend e2e** (Playwright, live): launch over a temp tree, list entries, open one
  with and without hot reload, assert the report renders and **Back to launcher**
  returns to `/`.
