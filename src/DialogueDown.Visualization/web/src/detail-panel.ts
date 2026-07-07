import type { DisplayNode } from "./model";
import { colorOf } from "./palette";
import { escapeHtml, renderMarkdown } from "./text";

export interface DetailPanel {
    show(node: DisplayNode): void;
    clear(): void;
}

/** The side panel showing a selected node's category, attributes, and source. */
export function createDetailPanel(): DetailPanel {
    const titleEl = document.getElementById("detail-title")!;
    const bodyEl = document.getElementById("detail-body")!;
    const placeholder =
        "<p>Click any node to see the source it was produced from, and a rendered preview.</p>";

    return {
        show(node) {
            titleEl.innerHTML = categoryDot(node.category) + escapeHtml(node.label);
            bodyEl.innerHTML = attributesTable(node.attributes) + sourceSection(node.source);
        },
        clear() {
            titleEl.textContent = "Node details";
            bodyEl.innerHTML = placeholder;
        },
    };

    // A colour dot ties the node to its legend colour without repeating a category
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
        if (typeof source !== "string") return "";
        return (
            `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
            `<h4>Preview</h4><div class="preview">${renderMarkdown(source)}</div>`
        );
    }
}
