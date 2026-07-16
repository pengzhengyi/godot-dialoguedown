import { describe, expect, it } from "vitest";
import { isTextEntryTarget } from "./text-entry";

/** Build a detached element tree and return the innermost node. */
function nest(...selectors: string[]): Element {
    let parent: Element | null = null;
    let leaf!: Element;
    for (const selector of selectors) {
        const [tag, attr] = selector.split("."); // e.g. "div.cm-content"
        const el = document.createElement(tag || "div");
        if (attr) el.classList.add(attr);
        parent?.appendChild(el);
        parent = el;
        leaf = el;
    }
    return leaf;
}

describe("isTextEntryTarget", () => {
    it("matches form controls", () => {
        expect(isTextEntryTarget(document.createElement("input"))).toBe(true);
        expect(isTextEntryTarget(document.createElement("textarea"))).toBe(true);
        expect(isTextEntryTarget(document.createElement("select"))).toBe(true);
    });

    it("matches the CodeMirror editable content inside .cm-editor", () => {
        const content = nest("div.cm-editor", "div.cm-scroller", "div.cm-content");
        expect(isTextEntryTarget(content)).toBe(true);
    });

    it("matches a contenteditable surface", () => {
        const el = document.createElement("div");
        el.setAttribute("contenteditable", "true");
        expect(isTextEntryTarget(el)).toBe(true);
    });

    it("does not match a graph node or a plain element", () => {
        expect(isTextEntryTarget(nest("svg", "g.node"))).toBe(false);
        expect(isTextEntryTarget(document.createElement("div"))).toBe(false);
    });

    it("does not match a button (buttons are not text entry)", () => {
        expect(isTextEntryTarget(document.createElement("button"))).toBe(false);
    });

    it("is false for a null target", () => {
        expect(isTextEntryTarget(null)).toBe(false);
    });
});
