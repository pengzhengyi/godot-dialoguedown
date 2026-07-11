import { describe, expect, it } from "vitest";
import { GraphCameraStore, type GraphViewState } from "./graph-camera";

const at = (k: number): GraphViewState => ({ transform: { k, x: 0, y: 0 }, collapsed: [] });

describe("GraphCameraStore", () => {
    it("loads a saved stage state back by title", () => {
        const store = new GraphCameraStore();
        store.save("Markdown AST", at(2));
        expect(store.load("Markdown AST")).toEqual(at(2));
    });

    it("overwrites the state for a stage on a later save", () => {
        const store = new GraphCameraStore();
        store.save("Dialogue AST", at(1));
        store.save("Dialogue AST", at(3));
        expect(store.load("Dialogue AST")?.transform?.k).toBe(3);
    });

    it("returns undefined for a stage it has never seen", () => {
        const store = new GraphCameraStore();
        expect(store.load("Desugared AST")).toBeUndefined();
    });

    it("keeps each stage's state independent", () => {
        const store = new GraphCameraStore();
        store.save("Markdown AST", { transform: { k: 2, x: 10, y: 20 }, collapsed: ["a"] });
        store.save("Dialogue AST", { transform: { k: 1, x: 0, y: 0 }, collapsed: [] });
        expect(store.load("Markdown AST")?.collapsed).toEqual(["a"]);
        expect(store.load("Dialogue AST")?.collapsed).toEqual([]);
    });
});
