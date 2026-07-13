import { describe, it, expect, beforeEach } from "vitest";
import { createEntityHighlighter } from "./entity-highlight";

/** Build a root with a keyed graph node and two table cells for the same scene, plus a decoy. */
function scene(): {
    root: HTMLElement;
    node: HTMLElement;
    row: HTMLElement;
    jump: HTMLElement;
    decoy: HTMLElement;
} {
    const root = document.createElement("div");
    const node = el("div", { "data-entity-key": "scene:the-market" });
    const row = el("div", { "data-entity-key": "scene:the-market" });
    const jump = el("div", { "data-ref-key": "scene:the-market" });
    const decoy = el("div", { "data-entity-key": "scene:the-forest" });
    root.append(node, row, jump, decoy);
    document.body.append(root);
    return { root, node, row, jump, decoy };
}

function el(tag: string, attrs: Record<string, string>): HTMLElement {
    const element = document.createElement(tag);
    for (const [name, value] of Object.entries(attrs)) element.setAttribute(name, value);
    return element;
}

const HIGHLIGHT = "entity-highlight";
const hover = (target: HTMLElement) =>
    target.dispatchEvent(new MouseEvent("pointerover", { bubbles: true }));

describe("createEntityHighlighter", () => {
    beforeEach(() => (document.body.innerHTML = ""));

    it("highlights every element sharing a hovered entity key, entity and ref alike", () => {
        const { root, node, row, jump, decoy } = scene();
        createEntityHighlighter(root);

        hover(row);

        expect(node.classList.contains(HIGHLIGHT)).toBe(true);
        expect(row.classList.contains(HIGHLIGHT)).toBe(true);
        expect(jump.classList.contains(HIGHLIGHT)).toBe(true); // a ref cell lights up too
        expect(decoy.classList.contains(HIGHLIGHT)).toBe(false); // a different scene does not
    });

    it("highlights from a ref cell as well as an entity element", () => {
        const { root, node, jump } = scene();
        createEntityHighlighter(root);

        hover(jump);

        expect(node.classList.contains(HIGHLIGHT)).toBe(true);
    });

    it("clears the previous highlight when moving to a different entity", () => {
        const { root, node, decoy } = scene();
        createEntityHighlighter(root);

        hover(node);
        expect(node.classList.contains(HIGHLIGHT)).toBe(true);

        hover(decoy);
        expect(node.classList.contains(HIGHLIGHT)).toBe(false);
        expect(decoy.classList.contains(HIGHLIGHT)).toBe(true);
    });

    it("clears on pointer leave", () => {
        const { root, node } = scene();
        createEntityHighlighter(root);

        hover(node);
        root.dispatchEvent(new MouseEvent("pointerleave", { bubbles: true }));

        expect(node.classList.contains(HIGHLIGHT)).toBe(false);
    });

    it("does nothing when hovering an unkeyed element", () => {
        const { root, node } = scene();
        const plain = el("div", {});
        root.append(plain);
        createEntityHighlighter(root);

        hover(plain);

        expect(node.classList.contains(HIGHLIGHT)).toBe(false);
    });
});
