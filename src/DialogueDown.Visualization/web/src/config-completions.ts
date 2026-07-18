import {
    autocompletion,
    acceptCompletion,
    completionKeymap,
    type CompletionSource,
} from "@codemirror/autocomplete";
import type { EditorState } from "@codemirror/state";
import { keymap } from "@codemirror/view";
import type { Extension } from "@codemirror/state";
import { completionsFrom } from "./editor-completions";

// Completion `type`s select the tooltip icon (styled in styles.css). The `[[speakers]]` table
// and the reserved tag name get their own icons; the structural keys reuse the Source editor's
// icons for the matching concept — a person for `name`, `@` for `id`, `#` for `tags`.
const TABLE = "dd-config-table";
const RESERVED = "dd-config-reserved";

/** The `[[speakers]]` table DialogueDown's config format defines. */
const SPEAKERS_TABLE = "speakers";

/** The structural keys of a `[[speakers]]` entry — the config schema, stable and client-side. */
const SPEAKER_KEYS: ReadonlyArray<{ label: string; type: string }> = [
    { label: "name", type: "dd-speaker" },
    { label: "id", type: "dd-speaker-id" },
    { label: "tags", type: "dd-tag" },
];

/** The name of the nearest TOML table/array-of-tables header at or above `lineNumber`, or null. */
function nearestTableAbove(state: EditorState, lineNumber: number): string | null {
    for (let n = lineNumber; n >= 1; n--) {
        const match = /^\s*\[\[?\s*([\w-]+)/.exec(state.doc.line(n).text);
        if (match) return match[1]!;
    }
    return null;
}

/** Complete a table header (a line starting with `[`) with `[[speakers]]`. */
export function tableHeaderCompletions(): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/\[+[\w-]*/);
        if (!match) return null;
        const line = context.state.doc.lineAt(match.from);
        // A header sits at the line start; a `[` mid-line (e.g. an inline array) is not one.
        if (line.text.slice(0, match.from - line.from).trim() !== "") return null;
        const options = [{ label: `[[${SPEAKERS_TABLE}]]`, type: TABLE }];
        return completionsFrom(context, match.from, options, /^\[*[\w-]*$/);
    };
}

/**
 * Complete a key position inside a `[[speakers]]` table with the structural keys and the
 * reserved tag names. A key position is the start of a line (only whitespace before the
 * cursor), before any `=`, and not a comment.
 */
export function speakerKeyCompletions(reservedTags: readonly string[]): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/[\w-]*/);
        if (!match) return null;
        // Nothing typed yet and not an explicit request: stay quiet rather than pop on every line.
        if (!context.explicit && match.from === context.pos) return null;
        const line = context.state.doc.lineAt(match.from);
        const before = line.text.slice(0, match.from - line.from);
        if (before.trim() !== "") return null; // not at a key position
        if (line.text.includes("=")) return null; // already a `key = value`
        if (line.text.trimStart().startsWith("#")) return null; // a comment
        if (nearestTableAbove(context.state, line.number) !== SPEAKERS_TABLE) return null;
        const options = [
            ...SPEAKER_KEYS.map((key) => ({ label: key.label, type: key.type })),
            ...reservedTags.map((tag) => ({ label: tag, type: RESERVED })),
        ];
        return completionsFrom(context, match.from, options, /^[\w-]*$/);
    };
}

/**
 * The config (TOML) editor's schema autocompletion: the `[[speakers]]` table header, and the
 * `name` / `id` / `tags` keys plus the reserved tag names at a key position inside a speaker
 * table. Bundles CodeMirror's completion keymap (Edit-only, like the Source editor) and adds
 * Tab as a second accept key.
 */
export function configCompletions(reservedTags: readonly string[] = []): Extension {
    return [
        autocompletion({
            override: [tableHeaderCompletions(), speakerKeyCompletions(reservedTags)],
        }),
        keymap.of([...completionKeymap, { key: "Tab", run: acceptCompletion }]),
    ];
}
