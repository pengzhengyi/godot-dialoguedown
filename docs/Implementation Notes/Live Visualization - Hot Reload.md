# Live Visualization — Hot Reload

> [!NOTE]
> Status: **implemented** (Component 1 of live visualization). This component adds
> a `visualize` command-line entry point and a **watch mode** that serves the
> report from a local server and refreshes the browser whenever the source file
> changes on disk. **Live editing** (an in-browser editor, Save, dirty tracking)
> and a **file launcher** (in-app document picker) follow as separate components
> and reuse the foundation laid here.

The compilation report is transparent but **frozen**: you run it once and get a
snapshot. When you are iterating on a `.dialogue.md` script, you want the report
to keep up — save the file, see the new AST. This component delivers that loop
without touching the offline report: a `visualize` command renders a file (as
today), and `--watch` keeps the rendered report in sync with the file on disk.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Architecture](#architecture)
- [Hot-reload flow](#hot-reload-flow)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [HTTP surface](#http-surface)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Security](#security)
- [Integration](#integration)
- [Testability](#testability)

## Goal and scope

Give the visualization a **command-line entry point** and a **watch mode** that
keeps the report live against a file on disk.

**In scope:**

- A `visualize` CLI over the existing renderer:
  - `visualize <file>` — **static mode**: compile the file, write a self-contained
    report, and open it in the browser (today's offline artifact, now one command).
  - `visualize <file> --watch` — **watch mode**: start a local server bound to the
    file, serve the live report, and **hot-reload** the browser on every on-disk
    change.
- A local **live server** (loopback only) that compiles the document, serves the
  report, watches the file, and **pushes** recompiled stages to the browser.
- A small **live client** in the existing frontend that, when served live,
  subscribes to server pushes and rebuilds the tabs in place (keeping your active
  tab), with an inline banner for compile or file errors.

**Out of scope (deferred to later components):**

- **Editing in the browser, Save, and dirty tracking** — Component 2 (Live
  Editing). This component's report is **read-only**; the file changes from your
  editor, not from the page.
- **Picking a document from within the app** — Component 3 (File Launcher).
- Any change to the offline, embedded `report.html` behavior.

## Ubiquitous language

The domain is a running server reflecting a file. These terms are used verbatim in
the note, code, tests, and CLI help.

| Term | Meaning |
| --- | --- |
| **Document** | The `.dialogue.md` source file on disk being visualized. |
| **Report** | The rendered visualization of a document (Source tab + stage graphs). |
| **Static mode** | One-shot render to a self-contained file, no server (today's behavior, now via the CLI). |
| **Live session** | A running server bound to one document, serving its report and pushing updates. |
| **Watch mode** | A live session that reflects **on-disk** changes; the report is read-only. |
| **Hot reload** | The cycle triggered by a document change: recompile, then push the fresh stages to the browser. |
| **Live client** | The browser-side code (active only when served live) that subscribes to pushes and updates the report. |

Terms introduced by later components — **buffer**, **dirty**, **save**, **live
mode** (Component 2) and **launcher** (Component 3) — are noted where this design
leaves a seam for them, but are not built here.

## Functionality checklist

- [x] `visualize <file>` renders a document and opens the self-contained report.
- [x] `visualize <file> -o <path>` writes the report to a path instead of opening.
- [x] `visualize <file> --watch` starts a loopback live session and opens the browser.
- [x] `--watch` prints the URL and keeps running until interrupted (Ctrl+C).
- [x] Saving the document in an external editor updates the browser within ~1s.
- [x] The report rebuilds in place on reload, preserving the active tab.
- [x] Deleting the document shows a banner, not a blank page; the session recovers
      when the file reappears (a later good save pushes a fresh `reload`).
- [x] Multiple open browser tabs all receive updates.
- [x] Missing file / not `.dialogue.md` / bad arguments fail with a clear message.

The design also called for a distinct inline banner on a **compile error** after a
change. On `main` today the single Markdown stage always parses (any text is valid
Markdown), so there is no compile-failure path to surface yet; the banner is wired
for document-read failures (missing file) and will cover compile errors unchanged
once a stage that can reject input (the Dialogue AST) lands.

## Architecture

A new project, **`DialogueDown.Visualization.Live`**, owns all web and CLI
concerns and depends on the existing render library. The render library stays free
of any web dependency.

```mermaid
flowchart LR
    subgraph CLI["visualize (System.CommandLine)"]
        direction TB
        static["static: render → temp file → open browser"]
        watch["--watch: run live session"]
    end

    subgraph Live["DialogueDown.Visualization.Live"]
        cli["VisualizeCli<br/>(StaticMode / WatchMode)"]
        server["LiveVisualizationServer<br/>(ASP.NET minimal API, loopback)"]
        session["LiveSession<br/>(compile + serialize + broadcast)"]
        watcher["DocumentWatcher<br/>(FileSystemWatcher + Debouncer)"]
        broadcaster["SseBroadcaster<br/>(fan-out to clients)"]
    end

    subgraph Render["DialogueDown.Visualization (existing)"]
        visualizer["CompilationVisualizer<br/>compile + render"]
    end

    Browser["Browser<br/>(report + live client)"]

    static --> visualizer
    watch --> server
    server --> visualizer
    watcher -->|document changed| server
    server --> broadcaster
    broadcaster -->|SSE push| Browser
    Browser -->|GET /, /api/*| server
```

The render library gains a **public** seam for the operations the server needs:
compile a source to stages, render the report HTML with a **live marker**, and
serialize the current document payload. `CompilationVisualizer` (previously
`internal`) is public for this.

## Hot-reload flow

```mermaid
sequenceDiagram
    participant Editor as Your editor
    participant Disk as Document (disk)
    participant Server as Live server
    participant Browser

    Browser->>Server: GET / (live report)
    Server-->>Browser: HTML + initial {path, source, stages} + live marker
    Browser->>Server: GET /api/events (SSE, stays open)
    Editor->>Disk: save changes
    Disk-->>Server: FileSystemWatcher event (debounced)
    Server->>Server: recompile (CompilationVisualizer)
    alt document readable
        Server-->>Browser: SSE reload {path, source, stages}
        Browser->>Browser: rebuild tabs, keep active tab
    else document missing/unreadable
        Server-->>Browser: SSE problem {message}
        Browser->>Browser: show inline banner
    end
```

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `VisualizeCli` | Build the `visualize` command (arguments and options); dispatch to static or watch mode. | `System.CommandLine`, `StaticMode`, `WatchMode` |
| `StaticMode` / `WatchMode` | The two run paths: render-to-file-and-open, and run a live session until cancelled. | `CompilationVisualizer`, `LiveVisualizationServer`, `DocumentWatcher` |
| `DocumentValidation` | Reject a missing file or wrong extension with a clear message. | — |
| `LiveVisualizationServer` | Build and run the loopback web app; map the endpoints; stream SSE. | ASP.NET, `LiveSession`, `SseBroadcaster` |
| `LiveSession` | Own one document: render its live HTML, serialize its payload, and broadcast a `reload`/`problem` on refresh. | `CompilationVisualizer`, `SseBroadcaster` |
| `DocumentWatcher` | Wrap `FileSystemWatcher`; **debounce** editor write bursts (via `Debouncer`) into one callback. | `FileSystemWatcher`, `Debouncer` |
| `SseBroadcaster` | Track connected SSE clients (one channel each); fan out an event to all; drop closed ones. | ASP.NET response streams |
| `LiveEvent` | A tagged push: an event name (`reload`/`problem`) and its JSON data. Payloads are serialized by `CompilationVisualizer.SerializeDocument` (reusing `DisplayGraph`). | `DisplayGraph` |
| `resolveReport` + `startLiveClient` (frontend) | `main.ts` reads the injected payload; when it is marked live, `startLiveClient` subscribes and drives updates — otherwise the static path is unchanged. | `EventSource`, `runApp` |
| `live-client.ts` (frontend) | Subscribe to `/api/events`; on `reload`, rebuild the report preserving the active tab; on `problem`, show the banner. | `AppController` (`runApp`) |

## HTTP surface

Loopback only. This component is **read-only**; Component 2 adds the write routes.

| Route | Purpose |
| --- | --- |
| `GET /` | The live report HTML: the embedded report with the initial `{ path, source, stages }` injected and the **live marker** set (so the client goes live). |
| `GET /api/document` | Current `{ path, source, stages }` — used by the client to re-sync on reconnect. |
| `GET /api/events` | **SSE** stream. Emits `reload { path, source, stages }` after a successful recompile and `problem { message }` when the document cannot be read. |

## Key design decisions

### D1 — A separate project keeps the render library web-free

The server needs ASP.NET, a file watcher, and a process host — none of which
belong in the render library (which is a diagnostics companion to a deliberately
dependency-light core). A new `DialogueDown.Visualization.Live` project holds all
of it and references the render library. The render library exposes only a small
public seam (compile, render-with-live-marker); it never learns about HTTP.

### D2 — ASP.NET Core minimal API over a hand-rolled listener

A minimal API gives JSON, static content, and streaming responses idiomatically,
and hosts cleanly on an ephemeral loopback port so the endpoints get real
integration tests over HTTP. A hand-rolled `HttpListener` would add no dependency
but cost boilerplate and testability. We prefer the mature, well-understood host.

### D3 — Server-Sent Events over WebSocket

Hot reload only needs the server to **push** ("recompiled — here are the new
stages"). Everything the client sends (later: save) is a normal HTTP request. SSE
is exactly one-way server→client over plain HTTP, with a built-in browser
`EventSource` that **auto-reconnects**. WebSocket's full-duplex channel would be
unused complexity. If Component 2 or 3 later needs rich bidirectional messaging,
this decision can be revisited in isolation.

### D4 — One build; the live client is dormant until marked live

The frontend stays a **single bundle**. The static report leaves the live marker
unset and behaves exactly as today; the live server sets it, and the small live
client (an `EventSource` subscription plus an in-place rebuild) activates. The
live client is a few kilobytes, so carrying it in the offline artifact is
negligible — and it keeps one build and one code path to test. (Component 2's
editor, CodeMirror, is large, so **that** component introduces lazy-loading; this
one does not need it.)

### D5 — Reload rebuilds in place, preserving the active tab

On a push, the client re-runs the report render against the new payload and
restores the previously active tab index, rather than doing a full
`location.reload()`. You keep your place (Source vs a stage tab) across saves,
which is the point of a live loop. Finer per-graph state — zoom/pan and which
nodes are expanded — resets on reload; preserving it is deferred (a possible
Component 2 refinement). A full reload remains the fallback if a rebuild ever
throws.

### D6 — Debounce the watcher; the session re-reads on each change

Editors save by writing several times, or writing a temp file and renaming, so
`FileSystemWatcher` fires bursts. `DocumentWatcher` **debounces** (~150 ms, via a
small `Debouncer`) and coalesces the burst into a single callback. The callback is
the session's refresh: it re-reads and recompiles the current file once, so the
compile step is decoupled from raw filesystem noise and always sees the latest
content. Splitting the timing into `Debouncer` keeps it deterministically
testable, apart from the filesystem.

### D7 — Static mode opens a temp report by default

`visualize <file>` with no flag renders to a temp file and opens the browser (the
common case), with `-o <path>` to write somewhere instead and `--no-open` to
suppress launching. This makes the everyday "just show me" path a single word
while keeping scripting options. In watch mode, `--port` pins the loopback port
(otherwise an ephemeral port is chosen and its URL printed).

## Error and boundary cases

| Case | Intended behavior |
| --- | --- |
| File does not exist / wrong extension | CLI exits with a clear message before starting a session. |
| Editor save burst (temp-write + rename) | Debounced and coalesced into one recompile of the final content. |
| Document deleted | Push a `problem` event; the browser shows a banner; the session stays alive and a later save pushes a fresh `reload`. |
| Rapid successive saves | Debounce; always compile the most recent content. |
| Multiple browser tabs / reconnect | Broadcast to all SSE clients; a reconnecting client re-syncs via `GET /api/document`. |
| Port in use (`--port`) | Kestrel fails to bind and the process reports the error; omit `--port` to take an ephemeral port. |
| Compile error after a change (future) | Deferred: today's single Markdown stage always parses, so there is no failure to surface. The `problem` event and banner are wired and will carry compile errors once a rejecting stage (Dialogue AST) lands. |

## Security

This is a **development tool**, and the note says so plainly:

- The server binds **loopback (`127.0.0.1`) only**, on an ephemeral port — never a
  public interface.
- No authentication; it reads exactly one document and serves its report. There
  are **no write routes** in this component (Component 2 adds writes and will
  tighten this: confining writes to the one document path).
- No arbitrary file serving: the frontend assets are the embedded bundle, and the
  only file read is the document the CLI was pointed at.

## Integration

- **Render library:** `CompilationVisualizer` becomes **public**; the library
  exposes a public seam to (a) compile a source to `IReadOnlyList<DisplayGraph>`,
  (b) render the **live** report HTML, and (c) serialize the current document
  payload (`{ path, source, stages }`) for the API and push events. The existing
  static `RenderHtmlReport(source)` is unchanged, and the JSON/HTML plumbing stays
  internal. The **live marker rides in the injected report payload** (`report.live`
  with the document path); when it is absent the report behaves exactly as the
  static artifact — no separate template slot.
- **Frontend:** `main.ts` gains a branch — live marker present ⇒ start the live
  client; otherwise the current static path (`window.__DD_REPORT__`) is untouched.
  `runApp` returns an `AppController` so both the initial render and a hot reload
  drive the same in-place rebuild.
- **Existing tests:** unchanged. The static renderer and its embedding keep their
  current behavior and assertions.
- **CI:** the .NET job already builds and tests the whole solution, so it now
  covers the `.Live` project; the Frontend job additionally builds the live server
  and runs the live e2e, which launches it via Playwright's `webServer`.

## Testability

Following the test pyramid, with the existing 100%-coverage bar as the target
(the Live project's own sources reach it; one async SSE-handler continuation line
is a coverage artifact — the tests disconnect mid-stream, so the read loop never
exits normally).

- **Server (.NET, real loopback):** each test starts a `LiveVisualizationServer`
  on an ephemeral `127.0.0.1` port and drives it with `HttpClient` — `GET /` serves
  a live-marked report; `GET /api/document` returns the payload; `GET /api/events`
  streams a `reload` after the session refreshes. New test project
  `DialogueDown.Visualization.Live.Tests`.
- **`Debouncer`:** trigger rapidly and assert one fire; trigger again after the
  delay and assert a second — timing tested apart from the filesystem.
- **`DocumentWatcher`:** point it at a temp file; write, and assert the debounced
  callback fires; write rapidly, and assert the callback is coalesced.
- **`SseBroadcaster`:** connect fake clients; assert fan-out and that a disposed
  subscriber is dropped and its channel completed.
- **CLI:** `System.CommandLine` parsing is unit-tested (static vs watch, `-o`,
  bad input); `StaticMode` and a watch-invoke test cover both run paths with an
  injected fake browser launcher.
- **Frontend (Vitest):** the live client with an injected fake `EventSource` — a
  `reload` rebuilds and preserves the active tab; a `problem` shows a banner.
- **Frontend (Playwright e2e):** a separate config starts the **real** `dotnet`
  live server against a temp document (`webServer`); the browser loads the page,
  the test modifies the file, and the report updates in place; deleting the file
  shows the banner. This is the project's first server-backed e2e.
