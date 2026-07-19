import { describe, it, expect, beforeEach } from "vitest";
import { rememberActiveTab, rememberedActiveTab } from "./active-tab";

describe("active tab persistence", () => {
    beforeEach(() => window.sessionStorage.clear());

    it("returns null before any tab is remembered", () => {
        expect(rememberedActiveTab()).toBeNull();
    });

    it("round-trips the remembered tab title", () => {
        rememberActiveTab("Dialogue AST");
        expect(rememberedActiveTab()).toBe("Dialogue AST");
    });

    it("keeps only the most recently remembered tab", () => {
        rememberActiveTab("Source");
        rememberActiveTab("Semantic Model");
        expect(rememberedActiveTab()).toBe("Semantic Model");
    });

    it("never throws and reads back null when the store throws", () => {
        const throwing = {
            getItem: () => {
                throw new Error("blocked");
            },
            setItem: () => {
                throw new Error("blocked");
            },
        } as unknown as Storage;
        expect(() => rememberActiveTab("Source", throwing)).not.toThrow();
        expect(rememberedActiveTab(throwing)).toBeNull();
    });
});
