import { renderDocument } from "./text";
import { highlightMarkdown } from "./highlight";

/** Bounds for the draggable split, as a fraction of the container width. */
const MIN_RATIO = 0.2;
const MAX_RATIO = 0.8;

/**
 * The Source tab: the whole source document as raw Markdown (left) beside a live
 * rendered preview (right), split down the middle like an editor's side-by-side
 * preview. A draggable divider re-proportions the two panes. Anchor links in the
 * preview (`[text](#slug)`) scroll to their headings, which carry GitHub-style
 * ids (see {@link renderDocument}).
 */
export function createSourceView(source: string): HTMLElement {
    const container = document.createElement("div");
    container.className = "source-view";

    const sourcePane = document.createElement("div");
    sourcePane.className = "source-pane";
    // A scrollable region needs keyboard access (focusable) and a label.
    sourcePane.tabIndex = 0;
    sourcePane.setAttribute("role", "region");
    sourcePane.setAttribute("aria-label", "Markdown source");
    const pre = document.createElement("pre");
    const code = document.createElement("code");
    code.className = "hljs language-markdown";
    // highlight.js escapes the source, so assigning its HTML is safe.
    code.innerHTML = highlightMarkdown(source);
    pre.appendChild(code);
    sourcePane.appendChild(pre);

    const divider = document.createElement("div");
    divider.className = "source-divider";
    divider.setAttribute("role", "separator");
    divider.setAttribute("aria-orientation", "vertical");
    divider.title = "Drag to resize";

    const preview = document.createElement("div");
    preview.className = "source-preview preview";
    preview.tabIndex = 0;
    preview.setAttribute("role", "region");
    preview.setAttribute("aria-label", "Preview");
    preview.innerHTML = renderDocument(source);

    container.append(sourcePane, divider, preview);
    initSplitDivider(container, sourcePane, divider);
    return container;
}

/** Wire the divider so dragging it re-proportions the source pane. */
function initSplitDivider(
    container: HTMLElement,
    sourcePane: HTMLElement,
    divider: HTMLElement,
): void {
    let dragging = false;

    divider.addEventListener("mousedown", (event) => {
        dragging = true;
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    document.addEventListener("mousemove", (event) => {
        if (!dragging) return;
        const bounds = container.getBoundingClientRect();
        if (bounds.width === 0) return;
        const ratio = (event.clientX - bounds.left) / bounds.width;
        const clamped = Math.max(MIN_RATIO, Math.min(MAX_RATIO, ratio));
        sourcePane.style.flexBasis = `${(clamped * 100).toFixed(2)}%`;
    });

    document.addEventListener("mouseup", () => {
        dragging = false;
        document.body.style.userSelect = "";
    });
}
