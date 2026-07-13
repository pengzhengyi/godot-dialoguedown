# Semantic Model Visualization Tab

> [!IMPORTANT]
> Status: **implemented** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md), showing the
> [Semantic Analyzer](./Semantic%20Analyzer.md)'s output). Unlike the AST stages, the
> semantic analyzer produces **several outputs, not one graph**: a scene tree plus a
> speaker table, an anchor table, and a jump-resolution table. So its tab is an
> **analytics layout** — the **scene tree** as the graph on the left, and the three
> tables stacked as **collapsible panels** down the right — with **cross-linking**, so
> hovering a scene or speaker in one place highlights it everywhere it appears.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see the
> visualization note's maturity caveat); the core engine stays the reviewed surface.

## Table of contents

- [Goal & scope](#goal--scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Design](#design)
  - [Payload shape](#payload-shape)
  - [Cross-linking by entity key](#cross-linking-by-entity-key)
  - [Layout](#layout)
  - [Flow](#flow)
- [Interfaces & abstractions](#interfaces--abstractions)
- [The three tables](#the-three-tables)
- [Key design decisions](#key-design-decisions)
- [Error & boundary cases](#error--boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Deferred enhancements](#deferred-enhancements)
- [Implementation checklist](#implementation-checklist)

## Goal & scope

The report shows the Markdown, Dialogue, and Desugared ASTs — each a single graph in
its own tab. The **semantic analyzer** now runs after desugar and exposes a
[`SemanticModel`](./Semantic%20Analyzer.md) on the compilation result, but the report
does not show it.

The semantic model is **not one graph**. It is a **scene tree** (the nested scenes)
plus three lookup tables the analysis resolved — **speakers**, **anchors**, and **jump
resolutions**. A single graph tab cannot represent that well. This component adds a
**Semantic** tab with an **analytics layout**:

- the **scene tree** as the graph in the main (left) area, reusing the existing
  interactive tree view; and
- the **speaker table**, **anchor table**, and **jump-resolution table** as
  **collapsible table panels** stacked down the right column.

The tab is **cross-linked**: hovering a scene or speaker in any table (or the graph)
highlights that same entity everywhere it appears — so a reader can see, for example,
which scene a jump resolves to, or where a speaker is defined.

**In scope:** the C# projection of the semantic model into a scene-tree graph plus the
three tables with entity keys; the TS analytics-layout tab (graph + stacked collapsible
tables) with cross-link highlighting; and wiring the tab through the existing report
payload. **Out of scope:** changing the analyzer or its model; the flow graph
(succession/choice/jump *edges*, a later component); editing from this tab; and
speaker-driven autocomplete (the next component, tracked as
[#71](https://github.com/pengzhengyi/godot-dialoguedown/issues/71)).

## Ubiquitous language

Reuses the [Semantic Analyzer](./Semantic%20Analyzer.md)'s terms so one concept keeps
one name across the analyzer, this tab, and the code.

| Term | Meaning |
| --- | --- |
| **Scene tree** | The nested scenes, shown as the tab's graph. A scene's node links to its anchor-table row. |
| **Speaker table** | Rows of resolved speakers (name, `@id`, tags, whether default). |
| **Anchor table** | Rows mapping a scene's `#slug` anchor to its scene. |
| **Jump-resolution table** | Rows of each jump and what it resolved to (a scene, a deferred file target, or unresolved). |
| **Entity** | A cross-referenced thing with a stable identity: a **scene** or a **speaker**. Cross-linking highlights an entity everywhere it appears. |
| **Entity key** | The stable string identifying an entity across the graph and tables, e.g. `scene:the-market`, `speaker:@guide`. A row *is* an entity (its `entityKey`); a cell may *reference* one (its `refKey`). |
| **Table panel** | One collapsible container in the right column holding a table; collapses to a labeled horizontal bar. |

## Functionality checklist

- [x] A **Semantic** tab appears after the Desugared AST tab.
- [x] Its main area shows the **scene tree** as an interactive graph (zoom, pan, fold,
      full screen, position memory) — reusing the existing tree view.
- [x] The right column stacks three **table panels**: speaker, anchor, jump resolution.
- [x] Each table panel **collapses to a horizontal bar** (title + row count) and expands
      again; the choice persists across reloads.
- [x] **Cross-linking:** hovering a scene row/cell (or a scene node in the graph)
      highlights that scene in the graph, the anchor table, and any jump that resolves to
      it; hovering a speaker highlights it across the tables.
- [x] The jump-resolution table shows each jump's **resolution kind** — a scene link, a
      deferred file target, or unresolved — with the scene link cross-referencing the
      scene entity.
- [x] The speaker table marks the **default** speaker and lists each speaker's **tags**.
- [x] Empty tables render a clear "none" state, not a blank panel.
- [x] The tab is read-only on every mode; the View/Edit toggle stays frozen (as on the
      other graph tabs).

## Design

### Payload shape

The report payload today is `{ mode, path, source, stages }`, where each `stage` is a
graph (`title`, `description`, `nodes`, `edges`). The semantic stage **is still a graph**
— the scene tree — so it reuses that, and adds its tables alongside:

- Extend `Stage` with an optional **`tables?: SemanticTable[]`**. A stage with no
  `tables` renders exactly as today (a plain graph tab). A stage with `tables` renders in
  the analytics layout.

This keeps one uniform tab model: the scene tree flows through the entire existing tree
view (camera memory, fold, full screen, cross-stage colors) for free, and the tables are
purely additive. (Alternative — a separate top-level `semantic` payload with a bespoke
tab — was rejected: it would duplicate the tree view and split the tab model.)

### Cross-linking by entity key

Cross-linking needs a **shared identity** for each entity that appears in more than one
place. The projection emits **entity keys**:

- A scene-tree **graph node** for a scene carries `entityKey = scene:<anchor>`.
- An **anchor-table** row carries `entityKey = scene:<anchor>` (the row *is* that scene).
- A **jump-resolution** row that resolved to a scene carries, on its target cell,
  `refKey = scene:<anchor>` (it *references* that scene).
- A **speaker-table** row carries `entityKey = speaker:<id-or-name>`.

The TS side builds one index from `entityKey`/`refKey` → the DOM elements carrying it.
Hovering any element with a key adds a `highlight` class to **every** element sharing that
key, across the graph and all tables; leaving clears it. No positional or title-matching
guesswork — the key is the single source of truth, mirroring how the model itself keys
scenes by anchor and speakers by name/id.

### Layout

```text
┌──────────────────────────────┬───────────────────────────┐
│                              │ ▸ Speakers (3)            │  ← collapsed bar
│        Scene tree            ├───────────────────────────┤
│        (graph: zoom,         │ ▾ Anchors (4)             │
│         pan, fold,           │   #the-market → The Market│  ← expanded table
│         full screen)         │   #the-forest → The Forest│
│                              ├───────────────────────────┤
│                              │ ▾ Jumps (2)               │
│                              │   [east] → The Market     │
└──────────────────────────────┴───────────────────────────┘
```

The right column reuses the **collapsible-panel** pattern already in the report (the
node-inspector collapse from the side-panels work): each table panel has a header bar with
a collapse toggle; collapsed, only the bar shows. The three panels stack and scroll
independently. For this tab, the tables **replace** the single node-detail inspector.

### Flow

```mermaid
flowchart LR
  model["SemanticModel<br/>(compilation result)"] --> proj["SemanticProjection (C#)"]
  proj --> tree["Scene-tree graph<br/>(nodes + edges, entity keys)"]
  proj --> tables["3 tables<br/>(rows + entity/ref keys)"]
  tree --> stage["Stage { nodes, edges, tables }"]
  tables --> stage
  stage --> json["report JSON"]
  json --> view["createSemanticView (TS)"]
  view --> graph["tree view (scene tree)"]
  view --> panels["stacked collapsible table panels"]
  graph --> link["entity-key highlight index"]
  panels --> link
```

## Interfaces & abstractions

| Type / function | Responsibility | Collaborators |
| --- | --- | --- |
| `SemanticProjection` (C#) | Project a `SemanticModel` into a scene-tree `DisplayGraph` (with scene entity keys) plus the three `SemanticTable`s. | `SceneTreeProjection`, `GraphWalk` |
| `SceneTreeProjection : INodeProjection<Scene>` (C#) | Describe/'`Neighbors`' a `Scene` for the graph walk; a scene node carries its `entityKey` and a `TypeName` ("Scene"/"Document"). | `GraphWalk` |
| `NodeDescription.TypeName` / `DisplayNode.TypeName` (C#) | Optional legend name for a node whose label is content (a scene title) rather than a type; the legend groups by it when present. | `GraphWalk`, `createLegend` |
| `SemanticTable` / `SemanticRow` / `SemanticCell` (C#) | A serializable table: title, columns, and rows of cells; a cell may carry `entityKey`/`refKey`. | `DisplayGraphJson` |
| `Stage.tables` (TS + C# payload) | Optional tables riding alongside a stage's graph; absent ⇒ a plain graph tab. | `addStageTab`, `DisplayGraphJson` |
| `createSemanticView` (TS) | Build the analytics tab: the scene-tree tree view in the main area + stacked collapsible table panels, wired for cross-link highlight. | `createTreeView`, `createTablePanel`, `createEntityHighlighter` |
| `createTablePanel` (TS) | Render one `SemanticTable` as a collapsible panel (header bar + table) carrying the cross-link keys. | `initCollapsiblePanel` |
| `createEntityHighlighter` (TS) | Index elements by entity key and toggle the shared `entity-highlight` class on hover. | — |

## The three tables

| Table | One row per | Columns | Entity / ref keys |
| --- | --- | --- | --- |
| **Speakers** | resolved speaker | Name, `@id`, Tags, Default | row `entityKey = speaker:<id-or-name>` |
| **Anchors** | scene with an anchor | Anchor (`#slug`), Scene (heading text), Level | row `entityKey = scene:<anchor>` |
| **Jump resolutions** | analyzed jump | Jump (label), Target (`#slug`), Resolves to (scene / file — deferred / unresolved) | "Resolves to" cell `refKey = scene:<anchor>` for a scene jump |

## Key design decisions

- **The scene tree is the stage's graph; tables ride in `Stage.tables`.** Reuses the whole
  tree view and keeps one tab model; a table-less stage is unchanged. (See
  [Payload shape](#payload-shape).)
- **Cross-link by entity key, not by matching.** The projection emits `scene:<anchor>` and
  `speaker:<id-or-name>` keys; the UI highlights by exact key. This mirrors the model's own
  keying and avoids brittle title/position matching. (See
  [Cross-linking](#cross-linking-by-entity-key).)
- **Reuse the collapsible-panel pattern for the table stack.** The report already has a
  collapse toggle with persistence; the three table panels reuse it rather than inventing a
  new affordance.
- **A dedicated `SemanticProjection`, exposed through the existing friend seam.** The model
  is `internal`; the visualization project already has friend access via
  `CompilationResult.Semantics`, so the projection lives on the visualization side with no
  new public surface. The scene-tree graph goes through `GraphWalk` like every other stage.
- **Hover-to-highlight for v1.** The user's spec is hover-driven; a sticky click-to-pin
  selection is a possible later enhancement (see
  [Deferred enhancements](#deferred-enhancements)).
- **A node's legend name can differ from its label.** A scene node labels itself by its
  **title** (so the tree reads as scenes), which would make the legend list every title.
  So a node carries an optional `TypeName` ("Scene", "Document") that the legend groups and
  labels by, falling back to the label for AST stages that label themselves by type.

## Error & boundary cases

| Case | Behavior |
| --- | --- |
| No scenes (flat document, root only) | The scene tree shows just the root; the anchor table shows its "No scenes." note. |
| No speakers beyond the default | The speaker table shows the default speaker row only. |
| No jumps | The jump-resolution table shows its "No jumps." note. |
| Jump resolved to a **file target** (deferred) | Row shows the file/anchor as text with a "deferred" note; **no** scene `refKey`. |
| **Unresolved** jump (empty target) | Row shows "unresolved"; no `refKey`. |
| A scene whose heading text is empty after slugging | Cannot happen — the analyzer rejects empty-slug headings upstream; the projection assumes a valid model. |
| Duplicate-looking headings | The analyzer already made anchors unique (or threw); the projection trusts the model's keys. |

## Integration

- **C#:** `CompilationVisualizer.BuildStages` appends the semantic stage after the three
  AST stages, projecting `result.Semantics` through `SemanticProjection`. `DisplayGraphJson`
  serializes the new `tables` field (omitted when null, like other optional payload fields).
- **TS:** `runApp`'s `addStageTab` routes a stage that has `tables` to `createSemanticView`
  instead of the plain graph path; the scene tree still uses `createTreeView`, so camera
  memory, fold, and full screen work unchanged. The tab freezes the View/Edit toggle like
  the other graph tabs, and hides the shared node-detail inspector (its tables are the
  detail).
- The report stays a single self-contained offline file; no new runtime dependency.

## Testability

- **C# projection** — unit-test `SemanticProjection` against a compiled sample: assert the
  scene-tree graph's nodes/edges and entity keys, and each table's rows/cells and
  entity/ref keys. `SceneTreeProjection` is tested through `GraphWalk` like the AST
  projections.
- **TS** — unit-test `createEntityHighlighter` (hovering a key highlights all elements
  sharing it) and the table/panel rendering (jsdom). End-to-end (Playwright): the Semantic
  tab shows the graph + three collapsible tables; hovering an anchor row highlights the
  matching scene node and jump row; a table collapses to its bar and reopens.
- Mirror the one-file-per-source layout; high, meaningful coverage as elsewhere.

## Deferred enhancements

Settled for v1, with these enhancements deliberately deferred:

1. **Click-to-pin selection.** v1 is **hover-to-highlight** only. A sticky
   click-to-pin selection (so a reader can move the mouse away while comparing) is a
   possible fast follow.
2. **Speaker ↔ scene links.** Speakers are not scene-tree nodes, so a speaker entity
   highlights across the **tables only**, not the graph. Tagging each scene node with the
   speakers that appear in it (so hovering a speaker lights up the scenes they speak in)
   is deferred — it needs the projection to walk each scene's blocks for speakers.
3. **A node-detail panel.** This tab has **no separate detail strip** — clicking a scene
   node cross-highlights its rows, and the tables are the detail. A detail strip could be
   added later if a scene needs its own source/preview here.

## Implementation checklist

- [x] C#: `SemanticTable`/`SemanticRow`/`SemanticCell` model + `SceneTreeProjection` +
      `SemanticProjection`; unit tests.
- [x] C#: `Stage.tables` in the payload; `DisplayGraphJson` serializes it; `BuildStages`
      appends the semantic stage; tests.
- [x] C#: `NodeDescription`/`DisplayNode` `TypeName` for clean scene-tree legend labels;
      `GraphWalk` copies it; tests.
- [x] TS: `Stage.tables` type; `createEntityHighlighter`; `createSemanticView` (graph +
      stacked collapsible tables); `addStageTab` routing; unit + e2e tests.
- [x] Styling for the analytics layout and table panels; help text for the Semantic tab.
- [x] Rebuild the committed `dist/report.html`; `CHANGELOG` + README touch-ups.
- [ ] **Explicit UI-correctness approval** (live preview) before merge.
