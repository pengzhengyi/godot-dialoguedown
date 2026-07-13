import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { writeReport } from "./report";
import type { Report } from "../src/model";

// A Semantic Model report: a two-scene tree beside the speaker, anchor, and
// jump-resolution tables, wired with the same cross-link keys the .NET projection
// emits. Its own fixture so the source-tab suite's tab/node counts are untouched.
const report: Report = {
    source: "# The Market\n\nGuide @guide: Welcome.\n\n=> [Leave](#the-square)\n\n# The Square\n",
    stages: [
        {
            title: "Semantic Model",
            description: "The semantic model the analyzer resolves.",
            nodes: [
                {
                    id: "n0",
                    label: "Document root",
                    attributes: [],
                    category: "document",
                    typeName: "Document",
                },
                {
                    id: "n1",
                    label: "The Market",
                    attributes: [
                        { name: "anchor", value: "#the-market" },
                        { name: "level", value: "1" },
                    ],
                    category: "structure",
                    typeName: "Scene",
                    entityKey: "scene:the-market",
                },
                {
                    id: "n2",
                    label: "The Square",
                    attributes: [
                        { name: "anchor", value: "#the-square" },
                        { name: "level", value: "1" },
                    ],
                    category: "structure",
                    typeName: "Scene",
                    entityKey: "scene:the-square",
                },
            ],
            edges: [
                { fromId: "n0", toId: "n1", kind: "Child" },
                { fromId: "n0", toId: "n2", kind: "Child" },
            ],
            tables: [
                {
                    title: "Speakers",
                    columns: ["Name", "@id", "Tags", "Default"],
                    emptyText: "No speakers are declared.",
                    rows: [
                        {
                            entityKey: "speaker:@guide",
                            cells: [
                                { text: "Guide", entityKey: "speaker:@guide", category: "speech" },
                                { text: "@guide" },
                                { text: "" },
                                { text: "" },
                            ],
                        },
                    ],
                },
                {
                    title: "Anchors",
                    columns: ["Anchor", "Scene", "Level"],
                    emptyText: "No anchors appear in this script.",
                    rows: [
                        {
                            entityKey: "scene:the-market",
                            cells: [
                                { text: "#the-market", category: "structure" },
                                { text: "The Market" },
                                { text: "1" },
                            ],
                        },
                        {
                            entityKey: "scene:the-square",
                            cells: [
                                { text: "#the-square", category: "structure" },
                                { text: "The Square" },
                                { text: "1" },
                            ],
                        },
                    ],
                },
                {
                    title: "Jump resolutions",
                    columns: ["Jump", "Target", "Resolves to"],
                    emptyText: "No jumps appear in this script.",
                    rows: [
                        {
                            cells: [
                                { text: "Leave", category: "jump" },
                                { text: "#the-square" },
                                {
                                    text: "→ The Square",
                                    refKey: "scene:the-square",
                                    category: "structure",
                                },
                            ],
                        },
                    ],
                },
            ],
        },
    ],
};

const url = writeReport(report);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await page.locator(".tab", { hasText: "Semantic Model" }).click();
});

test("lays the scene-tree graph beside the three stacked tables", async ({ page }) => {
    await expect(page.locator(".semantic-view")).toBeVisible();
    await expect(page.locator(".semantic-graph g.node")).toHaveCount(3);
    await expect(page.locator(".table-panel-title")).toHaveText([
        "Speakers",
        "Anchors",
        "Jump resolutions",
    ]);
    // The shared node-detail inspector is hidden — the tab has its own tables.
    await expect(page.locator("#app")).toHaveClass(/no-detail/);
    await expect(page.locator("#detail")).toBeHidden();
});

test("cross-links a scene across the graph, the anchor table, and a jump", async ({ page }) => {
    const stage = page.locator(".semantic-stage.active");
    // Hover the anchor row for The Square; the scene node and the jump that resolves to it
    // light up too.
    await stage.locator('tr[data-entity-key="scene:the-square"]').hover();

    await expect(stage.locator('g.node[data-entity-key="scene:the-square"]')).toHaveClass(
        /entity-highlight/,
    );
    await expect(stage.locator('td[data-ref-key="scene:the-square"]')).toHaveClass(
        /entity-highlight/,
    );
    // A different scene stays unhighlighted.
    await expect(stage.locator('g.node[data-entity-key="scene:the-market"]')).not.toHaveClass(
        /entity-highlight/,
    );
});

test("collapses and reopens a table from its header bar", async ({ page }) => {
    const speakers = page
        .locator(".table-panel")
        .filter({ has: page.locator(".table-panel-title", { hasText: "Speakers" }) });
    const header = speakers.locator(".table-panel-header");

    await expect(speakers.locator("tbody")).toBeVisible();
    await header.click();
    await expect(speakers).toHaveClass(/collapsed/);
    await expect(header).toHaveAttribute("aria-expanded", "false");
    await expect(speakers.locator("tbody")).toBeHidden();

    await header.click();
    await expect(speakers).not.toHaveClass(/collapsed/);
    await expect(speakers.locator("tbody")).toBeVisible();
});

test("has no accessibility violations on the Semantic tab", async ({ page }) => {
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});
