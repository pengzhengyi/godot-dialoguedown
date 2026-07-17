import { EditorState } from "@codemirror/state";
import { EditorView, lineNumbers } from "@codemirror/view";
import { StreamLanguage, syntaxHighlighting, HighlightStyle } from "@codemirror/language";
import { toml } from "@codemirror/legacy-modes/mode/toml";
import { tags } from "@lezer/highlight";
import type { ConfigReport, ConfiguredSpeakerView } from "./model";
import { isConfiguredFromFile } from "./model";
import { initSplitDivider } from "./source-view";
import { escapeHtml } from "./text";

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

/**
 * The Config tab: the applied configuration shown as a two-column split — the `dialogue.toml`
 * source (read-only, TOML-highlighted) on the left and the resolved configured speakers on
 * the right — reusing the Source tab's split machinery. When no config file was found it
 * shows a friendly explanation instead, because running on the built-in defaults is normal.
 */
export function createConfigView(config: ConfigReport): HTMLElement {
    const container = document.createElement("div");
    container.className = "config-view";

    const pane = document.createElement("div");
    pane.className = "config-source";

    const divider = document.createElement("div");
    divider.className = "config-divider";

    const side = document.createElement("div");
    side.className = "config-side";

    if (isConfiguredFromFile(config)) {
        mountReadOnlyEditor(pane, config.file!.source);
        side.appendChild(renderSpeakers(config.speakers));
    } else {
        pane.appendChild(renderNoConfigExplanation());
        side.appendChild(renderNoSpeakers());
    }

    container.append(pane, divider, side);
    initSplitDivider(container, divider, "--config-split");
    return container;
}

/** A focusable, read-only CodeMirror showing the TOML source. */
function mountReadOnlyEditor(parent: HTMLElement, source: string): void {
    new EditorView({
        parent,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                EditorState.readOnly.of(true),
                EditorView.editable.of(false),
                EditorView.contentAttributes.of({
                    "aria-label": "Configuration source",
                    tabindex: "0",
                }),
                StreamLanguage.define(toml),
                syntaxHighlighting(tomlHighlightStyle),
            ],
        }),
    });
}

/** The configured-speakers table: Name, @id, and tag chips colored by reserved vs custom. */
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
        `<th scope="col">Name</th><th scope="col">@id</th><th scope="col">Tags</th>` +
        `</tr></thead><tbody>${rows}</tbody>`;
    wrapper.appendChild(table);
    return wrapper;
}

function speakerCells(speaker: ConfiguredSpeakerView): string {
    const id = speaker.id ? escapeHtml(speaker.id) : `<span class="config-empty">—</span>`;
    const tags =
        speaker.tags.length === 0
            ? `<span class="config-empty">—</span>`
            : speaker.tags.map(tagChip).join(" ");
    return (
        `<td>${escapeHtml(speaker.name)}</td>` +
        `<td>${id}</td>` +
        `<td class="config-tags">${tags}</td>`
    );
}

/** One tag chip, marked reserved or custom so CSS colors the two apart. */
function tagChip(tag: { name: string; value?: string; reserved: boolean }): string {
    // Reserved tags are written with a double hash (`##default`), custom ones with a single.
    const prefix = tag.reserved ? "##" : "#";
    const label = tag.value == null ? `${prefix}${tag.name}` : `${prefix}${tag.name}=${tag.value}`;
    const kind = tag.reserved ? "reserved" : "custom";
    return `<span class="config-tag config-tag-${kind}">${escapeHtml(label)}</span>`;
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
