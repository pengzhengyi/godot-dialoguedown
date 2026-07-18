// @vitest-environment node

import { describe, expect, it } from "vitest";
import { GraphCameraStore } from "./graph-camera";

const camera = (k: number, x = 0, y = 0) => ({ k, x, y });

describe("GraphCameraStore", () => {
    it("uses the default framing (null) for an untouched graph with no shared camera", () => {
        const store = new GraphCameraStore();
        expect(store.cameraFor("Markdown AST")).toBeNull();
        expect(store.foldFor("Markdown AST")).toEqual([]);
    });

    it("pins a per-graph override and shares it as the current camera", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5));
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5)); // its own override
        expect(store.cameraFor("Dialogue AST")).toEqual(camera(1.5)); // untouched inherits current
    });

    it("keeps an adjusted graph's own camera while untouched graphs inherit the latest", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5));
        store.adjustCamera("Dialogue AST", camera(0.8));
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5)); // pinned, unaffected
        expect(store.cameraFor("Desugared AST")).toEqual(camera(0.8)); // untouched inherits latest
    });

    it("noteCamera moves the shared current without pinning an override", () => {
        const store = new GraphCameraStore();
        store.noteCamera(camera(2));
        expect(store.cameraFor("Markdown AST")).toEqual(camera(2)); // inherited
        store.adjustCamera("Markdown AST", camera(1.2)); // Markdown pins
        store.noteCamera(camera(2)); // current moves on
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.2)); // still pinned
        expect(store.cameraFor("Dialogue AST")).toEqual(camera(2)); // inherits the new current
    });

    it("remembers per-graph fold independently", () => {
        const store = new GraphCameraStore();
        store.setFold("Markdown AST", ["n1", "n2"]);
        expect(store.foldFor("Markdown AST")).toEqual(["n1", "n2"]);
        expect(store.foldFor("Dialogue AST")).toEqual([]);
    });

    it("reset drops the override (falls back to current) and clears the fold", () => {
        const store = new GraphCameraStore();
        store.adjustCamera("Markdown AST", camera(1.5)); // override = current = 1.5
        store.setFold("Markdown AST", ["n1"]);
        store.noteCamera(camera(3)); // current moves to 3
        expect(store.cameraFor("Markdown AST")).toEqual(camera(1.5)); // still pinned
        store.reset("Markdown AST");
        expect(store.cameraFor("Markdown AST")).toEqual(camera(3)); // now inherits current
        expect(store.foldFor("Markdown AST")).toEqual([]);
    });
});
