import { describe, it, expect, vi, beforeEach } from "vitest";
import { categoryStats, createLegend, type LegendHandlers } from "./legend";
import { CATEGORY_COLORS } from "./palette";
import type { DisplayNode, Stage } from "./model";

function node(id: string, label: string, category?: string): DisplayNode {
    return { id, label, category, attributes: [] };
}

describe("categoryStats", () => {
    it("counts nodes per category and collects distinct type names", () => {
        const nodes = [
            node("n1", "Heading (H1)", "structure"),
            node("n2", "Heading (H2)", "structure"),
            node("n3", "Code span", "call"),
        ];
        expect(categoryStats(nodes)).toEqual({
            structure: { names: ["Heading"], count: 2 },
            call: { names: ["Code span"], count: 1 },
        });
    });

    it("keeps multiple distinct type names within one category", () => {
        const nodes = [node("n1", "Heading (H1)", "structure"), node("n2", "Section", "structure")];
        expect(categoryStats(nodes).structure).toEqual({ names: ["Heading", "Section"], count: 2 });
    });

    it("ignores nodes without a category", () => {
        expect(categoryStats([node("n1", "Text")])).toEqual({});
    });
});

describe("createLegend", () => {
    let handlers: LegendHandlers;

    const stage: Stage = {
        title: "Markdown AST",
        edges: [],
        nodes: [
            node("n1", "Heading (H1)", "structure"),
            node("n2", "Heading (H2)", "structure"),
            node("n3", "Code span", "call"),
            node("n4", "Orphan"),
        ],
    };

    beforeEach(() => {
        handlers = { onToggle: vi.fn(), onHover: vi.fn(), onLeave: vi.fn() };
    });

    it("renders one row per present category, in palette order", () => {
        const legend = createLegend(stage, handlers);
        const labels = [...legend.querySelectorAll(".legend-label")].map((el) => el.textContent);
        // "structure" precedes "call" in CATEGORY_COLORS, and the uncategorised node is skipped.
        expect(Object.keys(CATEGORY_COLORS).indexOf("structure")).toBeLessThan(
            Object.keys(CATEGORY_COLORS).indexOf("call"),
        );
        expect(labels).toEqual(["Heading", "Code span"]);
    });

    it("shows a per-category node count and starts pressed (visible)", () => {
        const legend = createLegend(stage, handlers);
        const rows = legend.querySelectorAll("button.legend-item");
        expect(rows[0].querySelector(".count")?.textContent).toBe("2");
        expect(rows[0].getAttribute("aria-pressed")).toBe("true");
    });

    it("toggles a category off then on, reporting the dim state", () => {
        const legend = createLegend(stage, handlers);
        const row = legend.querySelector<HTMLButtonElement>("button.legend-item")!;

        row.click();
        expect(row.classList.contains("muted")).toBe(true);
        expect(row.getAttribute("aria-pressed")).toBe("false");
        expect(handlers.onToggle).toHaveBeenLastCalledWith("structure", true);

        row.click();
        expect(row.classList.contains("muted")).toBe(false);
        expect(row.getAttribute("aria-pressed")).toBe("true");
        expect(handlers.onToggle).toHaveBeenLastCalledWith("structure", false);
    });

    it("highlights on hover/focus and clears on leave/blur", () => {
        const legend = createLegend(stage, handlers);
        const row = legend.querySelector<HTMLButtonElement>("button.legend-item")!;

        row.dispatchEvent(new MouseEvent("mouseenter"));
        expect(handlers.onHover).toHaveBeenLastCalledWith("structure");
        row.dispatchEvent(new FocusEvent("focus"));
        expect(handlers.onHover).toHaveBeenCalledTimes(2);

        row.dispatchEvent(new MouseEvent("mouseleave"));
        row.dispatchEvent(new FocusEvent("blur"));
        expect(handlers.onLeave).toHaveBeenCalledTimes(2);
    });
});
