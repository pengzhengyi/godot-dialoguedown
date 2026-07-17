import { EditorState, Compartment } from "@codemirror/state";
import {
    EditorView,
    lineNumbers,
    keymap,
    drawSelection,
    highlightActiveLine,
    highlightActiveLineGutter,
} from "@codemirror/view";
import { history, historyKeymap, defaultKeymap } from "@codemirror/commands";
import { closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import {
    StreamLanguage,
    syntaxHighlighting,
    HighlightStyle,
    foldGutter,
    codeFolding,
    foldKeymap,
    foldService,
    bracketMatching,
} from "@codemirror/language";
import { search, searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { toml } from "@codemirror/legacy-modes/mode/toml";
import { tags } from "@lezer/highlight";
import type { ConfigReport, ConfiguredSpeakerView } from "./model";
import { isConfiguredFromFile } from "./model";
import { initSplitDivider } from "./source-view";
import { copyToClipboard } from "./path-display";
import { createMaximizeButton } from "./maximize-button";
import { initCollapsiblePanel } from "./collapse-toggle";
import { showToast } from "./toast";
import { escapeHtml } from "./text";

/** Options for the Config tab. */
export interface ConfigViewOptions {
    /** Toggle the whole-window maximize mode; when set, a maximize button is shown. */
    onToggleFullscreen?: () => void;
    /** Start the TOML editor editable (Edit) or read-only (View); toggled later via the handle. */
    editable?: boolean;
    /** Called with the new buffer on every edit — for the config buffer and dirty state. */
    onChange?: (source: string) => void;
}

/**
 * A handle to the Config tab so the live-edit machinery can drive it: flip the TOML editor
 * between editable and read-only, replace its content (a discard/restore), refresh the
 * configured speakers after a recompile, and signpost the pane as stale while unsaved.
 */
export interface ConfigViewHandle {
    /** The Config tab element to mount. */
    readonly element: HTMLElement;
    /** Switch the TOML editor between editable (Edit) and read-only (View) in place. */
    setEditable(editable: boolean): void;
    /** Replace the editor's content — used to restore the last saved version on discard. */
    setContent(source: string): void;
    /** Re-render the configured speakers from a freshly recompiled report. */
    updateSpeakers(config: ConfigReport): void;
    /** Mark the speakers pane as out of date (unsaved edits) or up to date. */
    setStale(stale: boolean): void;
}

/**
 * TOML highlighting driven by the same CSS variables the Markdown editor uses, so the config
 * source follows the page's light/dark theme live.
 */
const tomlHighlightStyle = HighlightStyle.define([
    { tag: [tags.keyword, tags.definition(tags.propertyName)], color: "var(--md-heading)" },
    { tag: tags.string, color: "var(--md-code)" },
    { tag: [tags.number, tags.bool, tags.atom], color: "var(--md-link)" },
    { tag: [tags.comment, tags.lineComment], color: "var(--md-muted)", fontStyle: "italic" },
    { tag: [tags.bracket, tags.squareBracket], color: "var(--md-muted)" },
]);

// Match a TOML table or array-of-tables header line, e.g. `[owner]` or `[[speakers]]`.
const TOML_HEADER = /^\s*\[/;

/**
 * Fold a TOML table/array-of-tables section: from the header line to the last line before the
 * next header (or the end of the file). The StreamLanguage TOML mode has no syntax-tree
 * folding, so a small line-based service supplies it — the analog of the Source editor's
 * heading fold.
 */
const foldTomlSections = foldService.of((state, lineStart) => {
    const header = state.doc.lineAt(lineStart);
    if (!TOML_HEADER.test(header.text)) return null;
    let end = header.number;
    for (let n = header.number + 1; n <= state.doc.lines; n++) {
        if (TOML_HEADER.test(state.doc.line(n).text)) break;
        end = n;
    }
    return end > header.number ? { from: header.to, to: state.doc.line(end).to } : null;
});

/**
 * The Config tab: the applied configuration shown as a two-column split — the `dialogue.toml`
 * source (read-only, TOML-highlighted) on the left and the resolved configured speakers on
 * the right — reusing the Source tab's split machinery. When no config file was found it
 * shows a friendly explanation instead, because running on the built-in defaults is normal.
 */
export function createConfigView(
    config: ConfigReport,
    options: ConfigViewOptions = {},
): ConfigViewHandle {
    const container = document.createElement("div");
    container.className = "config-view";

    const pane = document.createElement("div");
    pane.className = "config-source";

    const divider = document.createElement("div");
    divider.className = "config-divider";

    const side = document.createElement("div");
    side.className = "config-side";

    // A quiet, hidden-by-default hint that marks the speakers as out of date while the TOML
    // has unsaved edits (the speakers only refresh on Save — see the Live Edit note, DD6).
    const staleHint = renderStaleHint();

    let editor: EditorView | null = null;
    let speakers: HTMLElement | null = null;
    if (isConfiguredFromFile(config)) {
        editor = mountEditor(
            pane,
            config.file!.source,
            options.editable ?? false,
            options.onChange,
        );
        speakers = renderSpeakers(config.speakers);
        side.append(staleHint, speakers);
    } else {
        pane.appendChild(renderNoConfigExplanation());
        side.appendChild(renderNoSpeakers());
    }

    container.append(pane, divider, side);
    initSplitDivider(container, divider, "--config-split", "config-collapsed");

    // The right (speakers) panel can be hidden to give the config source the full width,
    // the same way the Source tab hides its preview. The toggle lives on the divider and
    // doubles as the always-present re-open handle; the choice is remembered across reloads.
    const speakersPanel = initCollapsiblePanel({
        container,
        collapsedClass: "config-collapsed",
        storageKey: "dd-config-collapsed",
        name: "configured speakers",
    });
    divider.appendChild(speakersPanel.button);

    // A maximize toggle in a small pill (bottom-right), matching the Source tab and the
    // graphs, so Config can fill the window with both its panes.
    if (options.onToggleFullscreen) {
        const controls = document.createElement("div");
        controls.className = "config-controls";
        controls.appendChild(createMaximizeButton(options.onToggleFullscreen));
        container.appendChild(controls);
    }

    return {
        element: container,
        setEditable: (next) =>
            editor?.dispatch({ effects: editability.reconfigure(editableConfig(next)) }),
        setContent: (next) =>
            editor?.dispatch({ changes: { from: 0, to: editor.state.doc.length, insert: next } }),
        updateSpeakers: (next) => {
            if (!speakers || !isConfiguredFromFile(next)) return;
            const fresh = renderSpeakers(next.speakers);
            speakers.replaceWith(fresh);
            speakers = fresh;
        },
        setStale: (stale) => {
            staleHint.hidden = !stale;
        },
    };
}

/** The read-only-in-View / editable-in-Edit extensions, flipped at runtime via this compartment. */
const editability = new Compartment();

function editableConfig(editable: boolean) {
    return [
        EditorState.readOnly.of(!editable),
        EditorView.contentAttributes.of(
            editable
                ? { "aria-label": "Configuration source editor", tabindex: "0" }
                : { "aria-label": "Configuration source", "aria-readonly": "true", tabindex: "0" },
        ),
        ...(editable ? [closeBrackets()] : []),
    ];
}

/** A focusable CodeMirror over the TOML source — read-only in View, editable in Edit. */
function mountEditor(
    parent: HTMLElement,
    source: string,
    editable: boolean,
    onChange?: (source: string) => void,
): EditorView {
    return new EditorView({
        parent,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                highlightActiveLineGutter(),
                foldGutter(),
                foldTomlSections,
                codeFolding(),
                history(),
                drawSelection(),
                highlightActiveLine(),
                highlightSelectionMatches(),
                bracketMatching(),
                search(),
                editability.of(editableConfig(editable)),
                StreamLanguage.define(toml),
                syntaxHighlighting(tomlHighlightStyle),
                EditorView.lineWrapping,
                keymap.of([
                    ...closeBracketsKeymap,
                    ...defaultKeymap,
                    ...historyKeymap,
                    ...searchKeymap,
                    ...foldKeymap,
                ]),
                EditorView.updateListener.of((update) => {
                    if (update.docChanged) onChange?.(update.state.doc.toString());
                }),
            ],
        }),
    });
}

/** The quiet "unsaved — save to refresh the speakers" hint (hidden until the config is dirty). */
function renderStaleHint(): HTMLElement {
    const hint = document.createElement("p");
    hint.className = "config-stale-hint";
    hint.hidden = true;
    hint.textContent = "Unsaved changes — save to refresh the speakers.";
    return hint;
}

/** Copy the text of a clicked cell or tag chip (any element carrying `data-copy`), and confirm it. */
function wireClickToCopy(root: HTMLElement): void {
    root.addEventListener("click", (event) => {
        const target = (event.target as Element | null)?.closest<HTMLElement>("[data-copy]");
        const value = target?.dataset.copy;
        if (!value) return;
        void copyToClipboard(value).then(() => showToast(`Copied ${value}`));
    });
}

/** A focusable, read-only CodeMirror showing the TOML source. */
/** The configured-speakers table: Name, Id, and tag chips colored by reserved vs custom. Every
 *  value is click-to-copy, so a writer can lift a name, `@id`, or tag straight into a script. */
function renderSpeakers(speakers: ConfiguredSpeakerView[]): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "config-speakers";
    wrapper.innerHTML = `<h4 class="config-speakers-heading">Configured speakers</h4>`;

    if (speakers.length === 0) {
        wrapper.appendChild(renderNoSpeakers());
        return wrapper;
    }

    const rows = speakers.map((speaker) => `<tr>${speakerCells(speaker)}</tr>`).join("");
    const table = document.createElement("table");
    table.className = "semantic-table config-speakers-table";
    table.innerHTML =
        `<thead><tr>` +
        `<th scope="col">Name</th><th scope="col">Id</th><th scope="col">Tags</th>` +
        `</tr></thead><tbody>${rows}</tbody>`;
    wireClickToCopy(table);
    wrapper.appendChild(table);
    return wrapper;
}

/** A value cell whose displayed text is exactly what a click copies (name, `@id`, …). */
function copyCell(text: string): string {
    const safe = escapeHtml(text);
    return `<td class="config-copy" data-copy="${safe}" title="Click to copy">${safe}</td>`;
}

function speakerCells(speaker: ConfiguredSpeakerView): string {
    // The id is shown (and copied) with its `@` sigil, exactly as a script references it.
    const id = speaker.id
        ? copyCell(`@${speaker.id}`)
        : `<td><span class="config-empty">—</span></td>`;
    const tags =
        speaker.tags.length === 0
            ? `<td><span class="config-empty">—</span></td>`
            : `<td class="config-tags">${speaker.tags.map(tagChip).join(" ")}</td>`;
    return copyCell(speaker.name) + id + tags;
}

/** One tag chip, marked reserved or custom so CSS colors the two apart; click-to-copy. */
function tagChip(tag: { name: string; value?: string; reserved: boolean }): string {
    // Reserved tags are written with a double hash (`##default`), custom ones with a single.
    const prefix = tag.reserved ? "##" : "#";
    const label = tag.value == null ? `${prefix}${tag.name}` : `${prefix}${tag.name}=${tag.value}`;
    const kind = tag.reserved ? "reserved" : "custom";
    const safe = escapeHtml(label);
    return (
        `<span class="config-tag config-tag-${kind}" data-copy="${safe}" title="Click to copy">` +
        `${safe}</span>`
    );
}

/** The friendly left pane when there is no config file — defaults are in play. */
function renderNoConfigExplanation(): HTMLElement {
    const note = document.createElement("div");
    note.className = "config-empty-state";
    note.innerHTML =
        `<p>This project has no <code>dialogue.toml</code>, so your script compiled with the ` +
        `built-in defaults.</p>` +
        `<p>Add one to declare speakers that every script can use — their names, ids, tags, ` +
        `and a default speaker.</p>`;
    return note;
}

/** The right pane's empty configured-speakers note. */
function renderNoSpeakers(): HTMLElement {
    const note = document.createElement("p");
    note.className = "config-empty";
    note.textContent = "No configured speakers yet.";
    return note;
}
