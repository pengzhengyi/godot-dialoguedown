import { describe, it, expect } from "vitest";
import { createSaveModeStore, defaultSaveMode } from "./save-mode";

function memoryStorage(seed: Record<string, string> = {}) {
    const map = new Map<string, string>(Object.entries(seed));
    return {
        map,
        storage: {
            getItem: (key: string) => map.get(key) ?? null,
            setItem: (key: string, value: string) => void map.set(key, value),
        },
    };
}

describe("createSaveModeStore", () => {
    it("defaults Source to Auto and Config to Manual", () => {
        const { storage } = memoryStorage();

        expect(createSaveModeStore("source", storage).get()).toBe("auto");
        expect(createSaveModeStore("config", storage).get()).toBe("manual");
    });

    it("reads a persisted mode over the default", () => {
        const { storage } = memoryStorage({ "dd-save-mode-source": "manual" });

        expect(createSaveModeStore("source", storage).get()).toBe("manual");
    });

    it("persists a mode under a per-document-type key", () => {
        const { map, storage } = memoryStorage();

        createSaveModeStore("config", storage).set("auto");

        expect(map.get("dd-save-mode-config")).toBe("auto");
        expect(createSaveModeStore("config", storage).get()).toBe("auto");
    });

    it("keeps Source and Config preferences independent", () => {
        const { storage } = memoryStorage();
        createSaveModeStore("source", storage).set("manual");
        createSaveModeStore("config", storage).set("auto");

        expect(createSaveModeStore("source", storage).get()).toBe("manual");
        expect(createSaveModeStore("config", storage).get()).toBe("auto");
    });

    it("falls back to the default for an unrecognized stored value", () => {
        const { storage } = memoryStorage({ "dd-save-mode-source": "sometimes" });

        expect(createSaveModeStore("source", storage).get()).toBe("auto");
    });

    it("falls back to the default when storage is unavailable", () => {
        expect(createSaveModeStore("source", null).get()).toBe("auto");
        expect(createSaveModeStore("config", null).get()).toBe("manual");
    });

    it("swallows storage read and write failures", () => {
        const throwing = {
            getItem: () => {
                throw new Error("blocked");
            },
            setItem: () => {
                throw new Error("blocked");
            },
        };
        const store = createSaveModeStore("source", throwing);

        expect(store.get()).toBe("auto");
        expect(() => store.set("manual")).not.toThrow();
    });
});

describe("defaultSaveMode", () => {
    it("reports each document type's default", () => {
        expect(defaultSaveMode("source")).toBe("auto");
        expect(defaultSaveMode("config")).toBe("manual");
    });
});
