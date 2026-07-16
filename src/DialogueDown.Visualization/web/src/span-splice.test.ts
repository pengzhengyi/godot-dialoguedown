import { describe, it, expect } from "vitest";
import { spanSplice } from "./span-splice";

const DOC = "# Scene\n\nGuide: Hi there.\n";

describe("spanSplice", () => {
    it("replaces the node's range with the new text", () => {
        // "Guide" occupies [9, 14); rename the speaker.
        expect(spanSplice(DOC, { start: 9, end: 14 }, "Herald")).toBe(
            "# Scene\n\nHerald: Hi there.\n",
        );
    });

    it("leaves the rest of the document untouched", () => {
        // Editing the heading keeps the body verbatim.
        expect(spanSplice(DOC, { start: 0, end: 7 }, "# Prologue")).toBe(
            "# Prologue\n\nGuide: Hi there.\n",
        );
    });

    it("only touches the node's range, not identical text elsewhere", () => {
        const doc = "Guide: Hi.\n\nGuide: Bye.\n";
        // The second "Guide" is at [12, 17); renaming it leaves the first alone.
        expect(spanSplice(doc, { start: 12, end: 17 }, "Elder")).toBe(
            "Guide: Hi.\n\nElder: Bye.\n",
        );
    });

    it("replacing the whole-document span rewrites the document", () => {
        expect(spanSplice(DOC, { start: 0, end: DOC.length }, "# New\n")).toBe("# New\n");
    });

    it("inserts at a zero-width span", () => {
        expect(spanSplice("ac", { start: 1, end: 1 }, "b")).toBe("abc");
    });

    it("is a faithful no-op when the text equals the sliced range", () => {
        const slice = DOC.slice(9, 14);
        expect(spanSplice(DOC, { start: 9, end: 14 }, slice)).toBe(DOC);
    });

    it("clamps offsets past the end of the document", () => {
        expect(spanSplice("ab", { start: 1, end: 99 }, "X")).toBe("aX");
    });

    it("orders reversed offsets so a stray span cannot corrupt the document", () => {
        // end < start would otherwise slice backwards; it is clamped to a zero-width insert.
        expect(spanSplice("abc", { start: 2, end: 1 }, "X")).toBe("abXc");
    });
});
