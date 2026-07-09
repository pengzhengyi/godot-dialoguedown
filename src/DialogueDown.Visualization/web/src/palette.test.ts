import { describe, it, expect } from "vitest";
import { CATEGORY_COLORS, DEFAULT_COLOR, colorOf } from "./palette";

describe("colorOf", () => {
    it("returns the mapped colour for a known category", () => {
        expect(colorOf("call")).toBe(CATEGORY_COLORS.call);
        expect(colorOf("structure")).toBe(CATEGORY_COLORS.structure);
    });

    it("falls back to the default colour for an unknown category", () => {
        expect(colorOf("nonsense")).toBe(DEFAULT_COLOR);
    });

    it("falls back to the default colour when the category is undefined", () => {
        expect(colorOf(undefined)).toBe(DEFAULT_COLOR);
    });
});

describe("CATEGORY_COLORS", () => {
    it("maps a code span and the game call it compiles to onto the same colour", () => {
        // The palette is intentionally coherent across stages: shared category name
        // (here "call") means a shared colour.
        expect(colorOf("call")).toBe(CATEGORY_COLORS.call);
    });

    it("gives every category a distinct colour", () => {
        const colours = Object.values(CATEGORY_COLORS);
        expect(new Set(colours).size).toBe(colours.length);
    });
});
