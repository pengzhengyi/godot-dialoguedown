// @vitest-environment node

import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { CompletionContext, type CompletionResult } from "@codemirror/autocomplete";
import {
    tableHeaderCompletions,
    speakerKeyCompletions,
    rootKeyCompletions,
    modeValueCompletions,
} from "./config-completions";

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

describe("rootKeyCompletions", () => {
    const source = rootKeyCompletions();

    it("offers the mode key at a root key position", () => {
        expect(labels(source(contextAt("m", 1)))).toEqual(["mode"]);
    });

    it("is quiet inside a [[speakers]] table", () => {
        const doc = "[[speakers]]\nm";
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([]);
    });

    it("is quiet in a value position (after =)", () => {
        const doc = "mode = s";
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([]);
    });

    it("is quiet in a comment", () => {
        const doc = "# m";
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([]);
    });

    it("gives the mode key its own icon type", () => {
        const result = source(contextAt("m", 1));
        const type = result && "options" in result ? result.options[0]?.type : undefined;
        expect(type).toBe("dd-config-mode");
    });
});

describe("modeValueCompletions", () => {
    const source = modeValueCompletions();

    it("offers the settable modes inside the value quotes", () => {
        const doc = 'mode = "';
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([
            "stage-boundary",
            "best-effort",
        ]);
    });

    it("offers the settable modes on an unquoted mode line, applying the quoted value", () => {
        const doc = "mode = ";
        const result = source(contextAt(doc, doc.length));
        expect(labels(result)).toEqual(["stage-boundary", "best-effort"]);
        const apply = result && "options" in result ? result.options[0]?.apply : undefined;
        expect(apply).toBe('"stage-boundary"');
    });

    it("is quiet on a non-mode value", () => {
        const doc = 'name = "';
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([]);
    });

    it("is quiet before the = (a key position)", () => {
        const doc = "mode";
        expect(labels(source(contextAt(doc, doc.length)))).toEqual([]);
    });
});
