import type { DisplayNode } from "./model";
import { colorOf } from "./palette";
import { escapeHtml, renderMarkdown } from "./text";

export interface DetailPanel {
    show(node: DisplayNode): void;
    clear(): void;
}

/** The body HTML shown when no node is selected. */
export const NODE_DETAIL_PLACEHOLDER =
    "<p>Click any node to see the source it was produced from, and a rendered preview.</p>";

/** The title HTML for a node's detail: a category color dot beside the node's label. */
export function nodeDetailTitle(node: DisplayNode): string {
    return categoryDot(node.category) + escapeHtml(node.label);
}

/** The body HTML for a node's detail: its attributes, then its source and a rendered preview. */
export function nodeDetailBody(node: DisplayNode): string {
    return attributesTable(node.attributes) + sourceSection(node.source);
}

/** The side panel showing a selected node's category, attributes, and source. */
export function createDetailPanel(): DetailPanel {
    const titleEl = document.getElementById("detail-title")!;
    const bodyEl = document.getElementById("detail-body")!;

    return {
        show(node) {
            titleEl.innerHTML = nodeDetailTitle(node);
            bodyEl.innerHTML = nodeDetailBody(node);
        },
        clear() {
            titleEl.textContent = "Node details";
            bodyEl.innerHTML = NODE_DETAIL_PLACEHOLDER;
        },
    };
}

// A color dot ties the node to its legend color without repeating a category
// name (the node's own label already appears beside it).
function categoryDot(category: string | undefined): string {
    if (!category) return "";
    return `<span class="dot" style="background:${colorOf(category)}"></span>`;
}

function attributesTable(attributes: DisplayNode["attributes"]): string {
    if (!attributes.length) return "";
    const rows = attributes
        .map(
            (attr) =>
                `<tr><th scope="row">${escapeHtml(attr.name)}</th><td>${escapeHtml(attr.value)}</td></tr>`,
        )
        .join("");
    return `<table><tbody>${rows}</tbody></table>`;
}

function sourceSection(source: string | undefined): string {
    // A node with no source is synthetic — a stage inserted it (a filled default
    // speaker), so it maps to no text. Say so, instead of an empty Source block.
    if (typeof source !== "string") {
        return `<p class="inserted-note">Inserted by the compiler — no source.</p>`;
    }
    return (
        `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
        `<h4>Preview</h4><div class="preview">${renderMarkdown(source)}</div>`
    );
}
