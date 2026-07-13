import { describe, it, expect, beforeEach } from "vitest";
import { createTablePanel } from "./semantic-table";
import type { SemanticTable } from "./model";

function speakerTable(): SemanticTable {
    return {
        title: "Speakers",
        columns: ["Name", "Id"],
        emptyText: "No speakers are declared.",
        rows: [
            {
                entityKey: "speaker:@guide",
                cells: [
                    { text: "Guide", entityKey: "speaker:@guide", category: "speech" },
                    { text: "@guide" },
                ],
            },
        ],
    };
}

function jumpTable(): SemanticTable {
    return {
        title: "Jump resolutions",
        columns: ["Label", "Target", "Resolves to"],
        emptyText: "No jumps appear in this script.",
        rows: [
            {
                cells: [
                    { text: "Enter" },
                    { text: "#the-market" },
                    { text: "The market", refKey: "scene:the-market", category: "jump" },
                ],
            },
        ],
    };
}

function emptyTable(): SemanticTable {
    return { title: "Anchors", columns: ["Anchor", "Scene"], emptyText: "No anchors.", rows: [] };
}

describe("createTablePanel", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("renders the title, the row count, and one column header per column", () => {
        const panel = createTablePanel(speakerTable());

        expect(panel.querySelector(".table-panel-title")?.textContent).toBe("Speakers");
        expect(panel.querySelector(".table-panel-count")?.textContent).toBe("1");
        expect([...panel.querySelectorAll("th")].map((th) => th.textContent)).toEqual([
            "Name",
            "Id",
        ]);
    });

    it("renders each cell's text and carries the row's entity key", () => {
        const panel = createTablePanel(speakerTable());
        const row = panel.querySelector("tbody tr");

        expect(row?.getAttribute("data-entity-key")).toBe("speaker:@guide");
        expect([...(row?.querySelectorAll("td") ?? [])].map((td) => td.textContent)).toEqual([
            "Guide",
            "@guide",
        ]);
    });

    it("tags an entity cell with its key and category", () => {
        const panel = createTablePanel(speakerTable());
        const cell = panel.querySelector("td");

        expect(cell?.getAttribute("data-entity-key")).toBe("speaker:@guide");
        expect(cell?.dataset.category).toBe("speech");
    });

    it("tags a reference cell with its ref key so the highlighter can cross-link it", () => {
        const panel = createTablePanel(jumpTable());
        const resolved = panel.querySelectorAll("td")[2];

        expect(resolved?.getAttribute("data-ref-key")).toBe("scene:the-market");
    });

    it("shows the empty note instead of a table when there are no rows", () => {
        const panel = createTablePanel(emptyTable());

        expect(panel.querySelector("table")).toBeNull();
        expect(panel.querySelector(".table-empty")?.textContent).toBe("No anchors.");
        expect(panel.querySelector(".table-panel-count")?.textContent).toBe("0");
    });

    it("collapses and reopens the panel when its header is pressed", () => {
        const panel = createTablePanel(speakerTable());
        const header = panel.querySelector<HTMLButtonElement>(".table-panel-header")!;
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(header.getAttribute("aria-expanded")).toBe("true");

        header.click();
        expect(panel.classList.contains("collapsed")).toBe(true);
        expect(header.getAttribute("aria-expanded")).toBe("false");

        header.click();
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(header.getAttribute("aria-expanded")).toBe("true");
    });
});
