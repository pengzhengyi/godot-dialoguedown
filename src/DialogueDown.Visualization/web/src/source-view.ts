import { EditorView, keymap, lineNumbers } from "@codemirror/view";
import { EditorState } from "@codemirror/state";
import { defaultKeymap, history, historyKeymap } from "@codemirror/commands";
import { markdown } from "@codemirror/lang-markdown";
import { syntaxHighlighting, defaultHighlightStyle } from "@codemirror/language";
import { renderDocument } from "./text";

/** Bounds for the draggable split, as a fraction of the container width. */
const MIN_RATIO = 0.2;
const MAX_RATIO = 0.8;

/** How the Source tab behaves — read-only in Static/Watch, an editor in Live Edit. */
export interface SourceViewOptions {
    /** When true the source pane is an editable editor (Live Edit); otherwise read-only. */
    editable?: boolean;
    /** Called (Live Edit) with the new buffer on every edit — for the preview and dirty state. */
    onChange?: (value: string) => void;
}

/**
 * The Source tab: a CodeMirror editor of the document (left) beside a live rendered
 * preview (right), split like an editor's side-by-side preview. The editor is read-only
 * in Static and Watch and editable in Live Edit, so the tab looks the same in every mode.
 * A draggable divider re-proportions the two panes; preview anchor links scroll to their
 * headings (see {@link renderDocument}).
 */
export function createSourceView(source: string, options: SourceViewOptions = {}): HTMLElement {
    const { editable = false, onChange } = options;

    const container = document.createElement("div");
    container.className = "source-view";

    const sourcePane = document.createElement("div");
    sourcePane.className = "source-pane";

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

    // Re-render the preview and report the new buffer on every edit (Live Edit only —
    // a read-only editor never fires a doc change).
    const onEdit = EditorView.updateListener.of((update) => {
        if (update.docChanged) {
            const value = update.state.doc.toString();
            preview.innerHTML = renderDocument(value);
            onChange?.(value);
        }
    });

    new EditorView({
        parent: sourcePane,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                history(),
                keymap.of([...defaultKeymap, ...historyKeymap]),
                markdown(),
                syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
                EditorView.lineWrapping,
                // Read-only (Static/Watch) keeps the editor focusable and selectable — it
                // just rejects edits — so the scrollable pane stays keyboard-accessible.
                EditorState.readOnly.of(!editable),
                EditorView.contentAttributes.of(
                    editable
                        ? { "aria-label": "Document source editor", tabindex: "0" }
                        : {
                              "aria-label": "Document source",
                              "aria-readonly": "true",
                              tabindex: "0",
                          },
                ),
                onEdit,
            ],
        }),
    });

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
