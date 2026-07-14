import { describe, it, expect } from "vitest";
import { hierarchy } from "d3";
import { lineageIds } from "./tree-view";

interface Node {
    id: string;
    kids?: Node[];
}

/** A small tree: root → (a → a1, a2), b. */
function sample() {
    return hierarchy<Node>(
        {
            id: "root",
            kids: [{ id: "a", kids: [{ id: "a1" }, { id: "a2" }] }, { id: "b" }],
        },
        (datum) => datum.kids,
    );
}

const child = (node: ReturnType<typeof sample>, id: string) =>
    node.children!.find((candidate) => candidate.data.id === id)!;

describe("lineageIds", () => {
    it("includes the node, its ancestors, and its descendants", () => {
        const a = child(sample(), "a");
        expect(lineageIds(a)).toEqual(new Set(["root", "a", "a1", "a2"]));
    });

    it("for the root, spans the whole tree", () => {
        expect(lineageIds(sample())).toEqual(new Set(["root", "a", "a1", "a2", "b"]));
    });

    it("for a leaf, is just its path to the root", () => {
        const a1 = child(child(sample(), "a"), "a1");
        expect(lineageIds(a1)).toEqual(new Set(["root", "a", "a1"]));
    });

    it("respects collapse — a collapsed node's hidden descendants are excluded", () => {
        const a = child(sample(), "a");
        // The tree view collapses by moving children to _children, leaving children undefined.
        (a as { children?: unknown }).children = undefined;
        expect(lineageIds(a)).toEqual(new Set(["root", "a"]));
    });
});
