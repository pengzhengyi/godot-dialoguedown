import {
    EditorView,
    keymap,
    lineNumbers,
    highlightActiveLine,
    highlightActiveLineGutter,
    drawSelection,
    rectangularSelection,
    crosshairCursor,
} from "@codemirror/view";
import { EditorState, EditorSelection, Prec } from "@codemirror/state";
import { defaultKeymap, history, historyKeymap } from "@codemirror/commands";
import { markdown } from "@codemirror/lang-markdown";
import {
    syntaxHighlighting,
    HighlightStyle,
    bracketMatching,
    foldGutter,
    codeFolding,
    foldKeymap,
    foldService,
} from "@codemirror/language";
import { search, searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import { tags } from "@lezer/highlight";
import { toggleWrap, insertLink, headingFoldEndLine } from "./editor-commands";
import { renderDocument } from "./text";

/**
 * Markdown syntax highlighting driven by CSS variables (`--md-*`), so the editor
 * follows the page's light/dark theme live — the colors resolve in the document, so
 * switching the theme re-colors the editor without rebuilding it. Strong and emphasis
 * keep the foreground color and lean on weight/slant, which reads in both themes.
 */
const markdownHighlightStyle = HighlightStyle.define([
    { tag: tags.heading, color: "var(--md-heading)", fontWeight: "600" },
    { tag: tags.strong, fontWeight: "700" },
    { tag: tags.emphasis, fontStyle: "italic" },
    { tag: [tags.link, tags.url], color: "var(--md-link)", textDecoration: "underline" },
    { tag: tags.monospace, color: "var(--md-code)" },
    { tag: tags.quote, color: "var(--md-muted)" },
    {
        tag: [tags.processingInstruction, tags.list, tags.contentSeparator],
        color: "var(--md-muted)",
    },
]);

/**
 * Fold Markdown sections: a heading folds everything down to the next heading of the
 * same or higher level (so a scene collapses to its `##` line). See
 * {@link headingFoldEndLine}.
 */
const foldHeadings = foldService.of((state, lineStart) => {
    const line = state.doc.lineAt(lineStart);
    const endLine = headingFoldEndLine((n) => state.doc.line(n).text, state.doc.lines, line.number);
    return endLine == null ? null : { from: line.to, to: state.doc.line(endLine).to };
});

/** Emphasis markers that surround a selection when typed over it (auto-surround). */
const EMPHASIS_MARKS = new Set(["*", "_", "~"]);

/**
 * Type `*`, `_`, or `~` over a selection to wrap it (e.g. select a word, press `*` →
 * `*word*`). Typing with no selection is left alone, so a lone marker stays literal.
 */
const emphasisSurround = EditorView.inputHandler.of((view, from, to, text) => {
    if (from === to || !EMPHASIS_MARKS.has(text)) return false;
    const selected = view.state.sliceDoc(from, to);
    view.dispatch(
        view.state.update({
            changes: { from, to, insert: `${text}${selected}${text}` },
            selection: EditorSelection.range(from + text.length, to + text.length),
            userEvent: "input",
        }),
    );
    return true;
});

/** VS Code-style Markdown formatting shortcuts (bold, italic, link). */
const formatKeymap = [
    { key: "Mod-b", run: toggleWrap("**"), preventDefault: true },
    { key: "Mod-i", run: toggleWrap("*"), preventDefault: true },
    { key: "Mod-k", run: insertLink, preventDefault: true },
];

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
                highlightActiveLineGutter(),
                foldGutter(),
                foldHeadings,
                codeFolding(),
                drawSelection(),
                EditorState.allowMultipleSelections.of(true),
                rectangularSelection(),
                crosshairCursor(),
                highlightActiveLine(),
                highlightSelectionMatches(),
                bracketMatching(),
                search(),
                history(),
                markdown(),
                syntaxHighlighting(markdownHighlightStyle),
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
                // Authoring aids only make sense when editable.
                ...(editable
                    ? [closeBrackets(), emphasisSurround, Prec.high(keymap.of(formatKeymap))]
                    : []),
                keymap.of([
                    ...closeBracketsKeymap,
                    ...defaultKeymap,
                    ...historyKeymap,
                    ...searchKeymap,
                    ...foldKeymap,
                ]),
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
