import type { Stage } from "./model";
import type { DisplayNode } from "./model";
import { createTreeView, type TreeView, type TreeViewOptions } from "./tree-view";
import { createTablePanel } from "./semantic-table";
import { createEntityHighlighter } from "./entity-highlight";

/** A built Semantic tab: its element to mount, and the scene-tree view for camera memory. */
export interface SemanticView {
    element: HTMLElement;
    view: TreeView;
}

/**
 * Build the Semantic tab's analytics layout: the scene tree as an interactive graph on the
 * left (reusing the tree view, so zoom, fold, full screen, and position memory work as on the
 * other tabs) and the speaker, anchor, and jump-resolution tables stacked as collapsible
 * panels on the right. Cross-link highlighting is wired across the whole tab, so hovering a
 * scene or speaker in any place lights it up everywhere it appears.
 */
export function createSemanticView(
    stage: Stage,
    onSelect: (node: DisplayNode) => void,
    options: TreeViewOptions,
): SemanticView {
    const container = document.createElement("div");
    container.className = "semantic-view";

    const graph = document.createElement("div");
    graph.className = "semantic-graph";

    const tables = document.createElement("div");
    tables.className = "semantic-tables";

    const view = createTreeView(stage, onSelect, options);
    graph.append(view.svg, view.legend, view.controls);
    for (const table of stage.tables ?? []) {
        tables.appendChild(createTablePanel(table));
    }

    container.append(graph, tables);
    createEntityHighlighter(container);
    return { element: container, view };
}
