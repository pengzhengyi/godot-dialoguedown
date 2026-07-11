# Graph Position Preservation

> [!NOTE]
> Status: **proposed** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md)). Each stage's
> graph remembers where the reader left it — its zoom, pan, and which branches are
> collapsed — so switching tabs and hot-reloading no longer snap every graph back
> to the default fit. The memory lives entirely in the browser; nothing new is
> serialized or sent to the server.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see
> the visualization note's maturity caveat); the core engine stays the reviewed
> surface.

## Table of contents

- [Goal](#goal)
- [Ubiquitous language](#ubiquitous-language)
- [Design](#design)
- [Key design decisions](#key-design-decisions)
- [Testability](#testability)
- [Implementation checklist](#implementation-checklist)

## Goal

The report shows one interactive graph per compiler stage, each in its own tab.
A reader zooms into a node, pans to a branch, and collapses noise to focus. Two
routine actions throw that work away today:

- **Switching tabs.** Activating a tab re-fits its graph to the container every
  time, so returning to a stage you had zoomed into resets it to the default view.
- **Hot-reloading.** A disk change (or a Live Edit save) rebuilds the stage graphs
  from scratch, so the reader's zoom, pan, and fold state vanish on every edit —
  exactly when staying put matters most.

Make each stage's graph **spatially stable**: remember its camera (zoom + pan) and
fold state, and re-apply them across tab switches and hot-reloads. A stage the
reader has never opened still auto-fits on first reveal; only a stage with a
remembered position keeps it.

In scope:

- Per-stage memory of the graph **camera** (zoom scale + pan translation).
- Per-stage memory of **fold state** (which nodes are collapsed).
- Re-applying both when a tab is re-activated and when the graphs are rebuilt on
  hot-reload, instead of re-fitting.

Out of scope:

- Persisting position across a full browser reload (F5). The memory is per page
  load, matching the report's other in-browser state (selection, theme aside).
- Remembering the selected node or the open detail panel across rebuilds
  (selection is still cleared on tab change, as today).
- Any change to the layout algorithm, the graphs' content, or the server payload.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Camera** | A graph's current zoom scale and pan translation — the D3 zoom transform `{ k, x, y }` applied to the viewport. |
| **Fold state** | The set of node ids currently collapsed (their children hidden) in a stage's tree. |
| **Graph view state** | A camera plus a fold state — everything needed to restore where a reader left one stage's graph. |
| **Camera store** | The in-browser memory that maps a stage (by its title) to its last graph view state. |
| **Fit** | (Existing.) Auto-scaling and centring a graph to its container. The default a stage falls back to when it has no remembered position. |
| **Stage title** | (Existing.) The stable tab label (`Markdown AST`, `Dialogue AST`, …) used as the camera store's key. |

## Design

The tree view already owns its camera (a D3 zoom behavior) and its fold state
(per-node `_children`). This enhancement adds two capabilities to it and a small
memory beside the tabs.

```text
  on switch-away / before rebuild        on re-activate / after rebuild
            │ getState()                          │ load(title)
            ▼                                     ▼
    ┌───────────────┐   save(title, state)   ┌─────────────┐
    │   TreeView    │ ─────────────────────▶ │ CameraStore │
    │ (zoom + fold) │ ◀───────────────────── │ title→state │
    └───────────────┘     restore(state)     └─────────────┘
                        (or fit if no memory)
```

### Tree view: read and restore its own position

`createTreeView` gains two methods on the returned `TreeView`:

- `getState(): GraphViewState` — snapshots the live D3 transform (`{ k, x, y }`)
  and the collapsed node ids.
- `restore(state): void` — collapses the named nodes, re-lays-out, and applies the
  transform (skipping the auto-fit).

It also accepts an optional `initialState` at construction: when a rebuilt graph
is handed its predecessor's state, it restores instead of fitting, so a
hot-reload never flashes through the default view.

Restoring fold state is **best-effort by node id**: only ids that still exist in
the rebuilt graph are collapsed; ids that changed (because the source changed) are
ignored. The camera is always restorable — it is absolute and does not depend on
node identity.

### Camera store: remember each stage

A small `GraphCameraStore` (a `Map` keyed by stage title) holds each stage's last
graph view state. It is pure and has no DOM or D3 dependency, so it is unit-tested
directly. The app owns one store for the life of the page.

### App wiring: snapshot then restore

`runApp` drives the store around the two loss points:

- **Tab switch.** Before changing the active tab, snapshot the outgoing graph view
  into the store. After revealing the new tab, restore its remembered state if the
  store has one; otherwise fit (first reveal). Activating a tab no longer
  unconditionally re-fits.
- **Hot-reload (`updateStages`).** Snapshot every existing graph view into the
  store before tearing the tabs down, then pass each rebuilt stage its remembered
  state so it restores on construction.

The Source tab has no graph and no camera, so it is skipped throughout.

## Key design decisions

- **D1 — Key the memory by stage title.** Titles (`Markdown AST`, `Dialogue AST`,
  …) are stable across rebuilds and unique per report, so they map a rebuilt stage
  back to its predecessor's position without threading ids through the payload.
- **D2 — In-browser, per page load.** The camera store is plain in-memory state.
  Position survives tab switches and hot-reloads (the two cases that lose it today)
  but resets on a full page reload, like the report's other view state. No
  `localStorage`, no payload change — the offline single-file report stays
  self-contained.
- **D3 — Fold state is best-effort; the camera is exact.** After an edit the tree's
  node ids can change, so only still-present collapsed ids are re-collapsed. The
  camera transform is identity-independent and always re-applies, which is the part
  a reader notices most.
- **D4 — Restore replaces fit, it does not fight it.** A stage fits only when it
  has no remembered camera (a first reveal, or a brand-new stage). Once
  remembered, the stage restores. This keeps the existing "fit on first reveal"
  behavior for never-opened tabs while making revisited tabs stable.
- **D5 — A pure store module for testability.** The memory is a dependency-free
  module so it is covered by fast unit tests; the DOM/D3 glue that reads and
  applies a position stays in the Playwright-tested tree view and app, matching
  how the rest of the browser-integration code is verified.

## Testability

- **Unit (Vitest).** The `GraphCameraStore` is covered directly: save then load
  a stage's state, overwrite it, and miss on an unknown stage. Being pure, it
  reaches full coverage without a DOM.
- **End-to-end (Playwright).** The camera-and-restore glue lives in `tree-view.ts`
  and `app.ts`, which are browser-integration modules exercised in a real browser.
  New e2e assertions:
  - Zoom a stage, switch tabs, switch back — the graph keeps its zoom (does not
    reset to fit).
  - Zoom a stage in a served session, trigger a hot-reload — the graph keeps its
    zoom after the rebuild.

## Implementation checklist

- [ ] `GraphCameraStore` (pure module) with `save` / `load`, plus the
      `CameraTransform` / `GraphViewState` types.
- [ ] `TreeView.getState()` / `TreeView.restore()` and an optional `initialState`
      on `createTreeView`.
- [ ] `runApp` snapshots on tab switch and before `updateStages`, and restores (or
      fits) on activate and rebuild.
- [ ] Vitest coverage for the store.
- [ ] Playwright e2e for zoom-kept-across-tab-switch and zoom-kept-across-reload.
- [ ] Rebuild the committed report bundle; confirm no unexpected drift.
- [ ] Crosscheck: flip status to implemented, reconcile deviations, add the index
      row.
