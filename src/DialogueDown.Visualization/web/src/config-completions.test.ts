// @vitest-environment node

import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { CompletionContext, type CompletionResult } from "@codemirror/autocomplete";
import { tableHeaderCompletions, speakerKeyCompletions } from "./config-completions";

function contextAt(doc: string, pos: number, explicit = false): CompletionContext {
    return new CompletionContext(EditorState.create({ doc }), pos, explicit);
}

// These sources are synchronous, so a non-null result is a plain CompletionResult.
function labels(result: CompletionResult | Promise<CompletionResult | null> | null): string[] {
    return result && "options" in result ? result.options.map((option) => option.label) : [];
}

describe("tableHeaderCompletions", () => {
    const source = tableHeaderCompletions();

    it("offers [[speakers]] at a line-leading bracket", () => {
        expect(labels(source(contextAt("[", 1)))).toEqual(["[[speakers]]"]);
    });

    it("offers [[speakers]] while typing the header", () => {
        expect(labels(source(contextAt("[[spea", 6)))).toContain("[[speakers]]");
    });

    it("is quiet for a mid-line bracket (an inline array)", () => {
        expect(source(contextAt("tags = [", 8))).toBeNull();
    });
});

describe("speakerKeyCompletions", () => {
    const source = speakerKeyCompletions(["default"]);

    it("offers the keys and reserved tags at a key position inside [[speakers]]", () => {
        const doc = "[[speakers]]\nn";
        expect(labels(source(contextAt(doc, doc.length)))).toEqual(
            expect.arrayContaining(["name", "id", "tags", "default"]),
        );
    });

    it("is quiet in a value position (after =)", () => {
        const doc = "[[speakers]]\nname = def";
        expect(source(contextAt(doc, doc.length))).toBeNull();
    });

    it("is quiet outside a [[speakers]] table", () => {
        const doc = "[[other]]\nn";
        expect(source(contextAt(doc, doc.length))).toBeNull();
    });

    it("is quiet in a comment", () => {
        const doc = "[[speakers]]\n# n";
        expect(source(contextAt(doc, doc.length))).toBeNull();
    });

    it("stays quiet with nothing typed unless the request is explicit", () => {
        const doc = "[[speakers]]\n";
        expect(source(contextAt(doc, doc.length, false))).toBeNull();
        expect(labels(source(contextAt(doc, doc.length, true)))).toContain("name");
    });

    it("offers only the structural keys when no reserved tags are supplied", () => {
        const doc = "[[speakers]]\nn";
        expect(labels(speakerKeyCompletions([])(contextAt(doc, doc.length)))).toEqual([
            "name",
            "id",
            "tags",
        ]);
    });

    it("gives each suggestion an icon type (reusing the Source editor's for the keys)", () => {
        const doc = "[[speakers]]\nn";
        const result = speakerKeyCompletions(["default"])(contextAt(doc, doc.length));
        const byLabel = Object.fromEntries(
            (result && "options" in result ? result.options : []).map((o) => [o.label, o.type]),
        );
        expect(byLabel).toMatchObject({
            name: "dd-speaker",
            id: "dd-speaker-id",
            tags: "dd-tag",
            default: "dd-config-reserved",
        });
    });
});
