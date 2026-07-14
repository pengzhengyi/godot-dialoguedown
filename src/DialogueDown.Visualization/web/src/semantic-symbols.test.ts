import { describe, it, expect } from "vitest";
import type { DialogueSymbols } from "./dialogue-symbols";
import { createSemanticSymbolSource } from "./semantic-symbols";

const EMPTY: DialogueSymbols = { jumpTargets: [], speakers: [], speakerIds: [], tags: [] };

describe("createSemanticSymbolSource", () => {
    it("falls back to the scan when no semantic symbols are present", () => {
        const scanned: DialogueSymbols = {
            jumpTargets: [{ slug: "market", heading: "Market" }],
            speakers: ["Alice"],
            speakerIds: ["alice"],
            tags: ["wise"],
        };
        const source = createSemanticSymbolSource(
            () => undefined,
            () => scanned,
        );

        expect(source("anything")).toEqual(scanned);
    });

    it("passes the live document to the scan on every call", () => {
        const seen: string[] = [];
        const source = createSemanticSymbolSource(
            () => EMPTY,
            (doc) => {
                seen.push(doc);
                return EMPTY;
            },
        );

        source("first");
        source("second");

        expect(seen).toEqual(["first", "second"]);
    });

    it("leads each list with the resolved symbols, then appends scan-only names", () => {
        const semantic: DialogueSymbols = {
            jumpTargets: [{ slug: "market", heading: "The Market" }],
            speakers: ["Guide"],
            speakerIds: ["guide"],
            tags: ["wise"],
        };
        const scanned: DialogueSymbols = {
            jumpTargets: [{ slug: "mill", heading: "The Mill" }],
            speakers: ["Alice"],
            speakerIds: ["alice"],
            tags: ["angry"],
        };
        const source = createSemanticSymbolSource(
            () => semantic,
            () => scanned,
        );

        const merged = source("doc");

        expect(merged.jumpTargets).toEqual([
            { slug: "market", heading: "The Market" },
            { slug: "mill", heading: "The Mill" },
        ]);
        expect(merged.speakers).toEqual(["Guide", "Alice"]);
        expect(merged.speakerIds).toEqual(["guide", "alice"]);
        expect(merged.tags).toEqual(["wise", "angry"]);
    });

    it("keeps the resolved jump target when the scan repeats its slug", () => {
        const semantic: DialogueSymbols = {
            ...EMPTY,
            jumpTargets: [{ slug: "market", heading: "Resolved Market" }],
        };
        const scanned: DialogueSymbols = {
            ...EMPTY,
            jumpTargets: [{ slug: "market", heading: "Scanned Market" }],
        };
        const source = createSemanticSymbolSource(
            () => semantic,
            () => scanned,
        );

        expect(source("doc").jumpTargets).toEqual([{ slug: "market", heading: "Resolved Market" }]);
    });

    it("deduplicates names shared by the resolved symbols and the scan", () => {
        const shared: DialogueSymbols = {
            jumpTargets: [],
            speakers: ["Guide"],
            speakerIds: ["guide"],
            tags: ["wise"],
        };
        const source = createSemanticSymbolSource(
            () => shared,
            () => shared,
        );

        const merged = source("doc");

        expect(merged.speakers).toEqual(["Guide"]);
        expect(merged.speakerIds).toEqual(["guide"]);
        expect(merged.tags).toEqual(["wise"]);
    });

    it("reads the semantic holder on every call so a hot-reload can refresh it", () => {
        let current: DialogueSymbols | undefined = undefined;
        const source = createSemanticSymbolSource(
            () => current,
            () => EMPTY,
        );

        expect(source("doc").speakers).toEqual([]);

        current = { ...EMPTY, speakers: ["Merchant"] };
        expect(source("doc").speakers).toEqual(["Merchant"]);
    });
});
