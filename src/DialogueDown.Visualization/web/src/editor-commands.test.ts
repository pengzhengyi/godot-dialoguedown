import { describe, it, expect } from "vitest";
import { EditorState, EditorSelection, type StateCommand } from "@codemirror/state";
import { toggleWrap, insertLink, headingFoldEndLine } from "./editor-commands";

/** Run a command over a doc + selection and return the resulting doc and primary selection. */
function run(
    command: StateCommand,
    doc: string,
    anchor: number,
    head = anchor,
): { doc: string; from: number; to: number } {
    const state = EditorState.create({ doc, selection: EditorSelection.single(anchor, head) });
    let next = state;
    command({ state, dispatch: (tr) => (next = tr.state) });
    const range = next.selection.main;
    return { doc: next.doc.toString(), from: range.from, to: range.to };
}

describe("toggleWrap", () => {
    const bold = toggleWrap("**");

    it("wraps the selection and keeps it selected", () => {
        const result = run(bold, "say hello now", 4, 9); // "hello"
        expect(result.doc).toBe("say **hello** now");
        expect([result.from, result.to]).toEqual([6, 11]); // still "hello"
    });

    it("unwraps an already-bold selection", () => {
        const result = run(bold, "say **hello** now", 6, 11); // inside the markers
        expect(result.doc).toBe("say hello now");
        expect([result.from, result.to]).toEqual([4, 9]);
    });

    it("inserts an empty pair with the cursor between on an empty selection", () => {
        const result = run(bold, "a b", 2);
        expect(result.doc).toBe("a ****b");
        expect([result.from, result.to]).toEqual([4, 4]); // between the markers
    });

    it("italic uses a single marker", () => {
        expect(run(toggleWrap("*"), "hi there", 0, 2).doc).toBe("*hi* there");
    });

    it("does nothing on a read-only document", () => {
        const state = EditorState.create({
            doc: "hello",
            selection: EditorSelection.single(0, 5),
            extensions: EditorState.readOnly.of(true),
        });
        let dispatched = false;
        expect(bold({ state, dispatch: () => (dispatched = true) })).toBe(false);
        expect(dispatched).toBe(false);
    });
});

describe("insertLink", () => {
    it("wraps a selection and puts the cursor in the parentheses", () => {
        const result = run(insertLink, "see the market here", 8, 14); // "market"
        expect(result.doc).toBe("see the [market]() here");
        expect(result.from).toBe(8 + 1 + "market".length + 2); // between ( )
        expect(result.to).toBe(result.from);
    });

    it("inserts an empty link with the cursor in the brackets on empty selection", () => {
        const result = run(insertLink, "x ", 2);
        expect(result.doc).toBe("x []()");
        expect(result.from).toBe(3); // inside the []
    });
});

describe("headingFoldEndLine", () => {
    const lines = [
        "# Scene", // 1
        "", // 2
        "Alice: hi.", // 3
        "## Buy", // 4
        "one coin.", // 5
        "## Leave", // 6
        "bye.", // 7
    ];
    const at = (n: number) => lines[n - 1];

    it("folds a top heading through to the end of the document", () => {
        expect(headingFoldEndLine(at, lines.length, 1)).toBe(7);
    });

    it("stops a sub-section at the next same-level heading", () => {
        expect(headingFoldEndLine(at, lines.length, 4)).toBe(5); // ## Buy ends before ## Leave
    });

    it("returns null for a non-heading line", () => {
        expect(headingFoldEndLine(at, lines.length, 3)).toBeNull();
    });

    it("returns null for a heading with an empty body", () => {
        const only = ["# Alone"];
        expect(headingFoldEndLine((n) => only[n - 1], only.length, 1)).toBeNull();
    });
});
