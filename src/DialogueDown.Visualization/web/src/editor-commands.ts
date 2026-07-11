import { EditorSelection, type StateCommand } from "@codemirror/state";

/**
 * A command that toggles a wrapping marker (e.g. `**` for bold, `*` for italic) around
 * each selection range. If a range is already wrapped by the marker it is unwrapped;
 * otherwise the marker is added on both sides. With an empty selection it inserts the
 * pair and drops the cursor between them, so ⌘B on nothing gives `**|**`.
 */
export function toggleWrap(marker: string): StateCommand {
    const len = marker.length;
    return ({ state, dispatch }) => {
        if (state.readOnly) return false;
        const changes = state.changeByRange((range) => {
            const before = state.sliceDoc(Math.max(0, range.from - len), range.from);
            const after = state.sliceDoc(range.to, Math.min(state.doc.length, range.to + len));
            if (range.from !== range.to && before === marker && after === marker) {
                return {
                    changes: [
                        { from: range.from - len, to: range.from },
                        { from: range.to, to: range.to + len },
                    ],
                    range: EditorSelection.range(range.from - len, range.to - len),
                };
            }
            return {
                changes: [
                    { from: range.from, insert: marker },
                    { from: range.to, insert: marker },
                ],
                range: EditorSelection.range(range.from + len, range.to + len),
            };
        });
        dispatch(state.update(changes, { userEvent: "input", scrollIntoView: true }));
        return true;
    };
}

/**
 * A command that wraps each selection as a Markdown link `[text]()`, leaving the cursor
 * where you type next: inside the `()` when there was selected text (paste the URL),
 * inside the `[]` when the selection was empty (type the label first).
 */
export const insertLink: StateCommand = ({ state, dispatch }) => {
    if (state.readOnly) return false;
    const changes = state.changeByRange((range) => {
        const text = state.sliceDoc(range.from, range.to);
        const cursor = range.from === range.to ? range.from + 1 : range.from + 1 + text.length + 2;
        return {
            changes: { from: range.from, to: range.to, insert: `[${text}]()` },
            range: EditorSelection.cursor(cursor),
        };
    });
    dispatch(state.update(changes, { userEvent: "input", scrollIntoView: true }));
    return true;
};

const HEADING = /^(#{1,6})\s/;

/**
 * The last line of the section a heading opens: everything up to (but not including) the
 * next heading of the same or higher level, or the end of the document. Returns `null`
 * when the given line is not a heading or the section is empty (nothing to fold).
 *
 * Line numbers are 1-based to match CodeMirror's `Text` API.
 */
export function headingFoldEndLine(
    lineText: (lineNumber: number) => string,
    lineCount: number,
    headingLine: number,
): number | null {
    const match = HEADING.exec(lineText(headingLine));
    if (!match) return null;
    const level = match[1].length;
    let end = headingLine;
    for (let n = headingLine + 1; n <= lineCount; n++) {
        const next = HEADING.exec(lineText(n));
        if (next && next[1].length <= level) break;
        end = n;
    }
    return end > headingLine ? end : null;
}
