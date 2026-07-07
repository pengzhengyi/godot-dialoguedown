import { delegate } from "tippy.js";

/**
 * Rich, accessible hover tooltips (Tippy.js) over the graph nodes, showing a
 * node's full label and attributes (from its `data-tip`). Delegation covers
 * nodes added later on expand.
 */
export function initTooltips(parent: Element): void {
    delegate(parent, {
        target: "g.node",
        allowHTML: true,
        maxWidth: 340,
        delay: [120, 0],
        content: (reference) => reference.getAttribute("data-tip") ?? "",
    });
}
