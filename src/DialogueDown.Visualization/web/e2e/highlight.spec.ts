import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Report } from "../src/model";

// "Alice @alice #happy: Hi." on line 0 — the speaker's name, @id, tag, and : separator as
// disjoint tokens — and a jump indicator on line 1.
const REPORT: Report = {
    source: "Alice @alice #happy: Hi.\n=> #scene-2\n",
    stages: SAMPLE_STAGES,
    semanticTokens: [
        {
            range: { start: { line: 0, character: 0 }, end: { line: 0, character: 5 } },
            kind: "SpeakerName",
        },
        {
            range: { start: { line: 0, character: 6 }, end: { line: 0, character: 12 } },
            kind: "SpeakerId",
        },
        {
            range: { start: { line: 0, character: 13 }, end: { line: 0, character: 19 } },
            kind: "CustomTag",
        },
        {
            range: { start: { line: 0, character: 19 }, end: { line: 0, character: 20 } },
            kind: "Separator",
        },
        {
            range: { start: { line: 1, character: 0 }, end: { line: 1, character: 2 } },
            kind: "JumpIndicator",
        },
    ],
};

const url = writeReport(REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

test("colors the speaker's name, id, separator, tag, and jump distinctly", async ({ page }) => {
    const active = page.locator("section.stage.active");

    await expect(active.locator(".dd-tok-speaker-name")).toHaveText("Alice");
    await expect(active.locator(".dd-tok-speaker-id")).toHaveText("@alice");
    await expect(active.locator(".dd-tok-custom-tag")).toHaveText("#happy");
    await expect(active.locator(".dd-tok-separator")).toHaveText(":");
    await expect(active.locator(".dd-tok-jump")).toHaveText("=>");

    // Each kind resolves to its own color (the theme's token variables) — five distinct.
    const colors = await active.evaluate((root) => {
        const colorOf = (selector: string) =>
            getComputedStyle(root.querySelector(selector) as Element).color;
        return [
            colorOf(".dd-tok-speaker-name"),
            colorOf(".dd-tok-speaker-id"),
            colorOf(".dd-tok-separator"),
            colorOf(".dd-tok-custom-tag"),
            colorOf(".dd-tok-jump"),
        ];
    });
    expect(new Set(colors).size).toBe(5);
});

test("keeps the tag a separate token, not nested inside a speaker token", async ({ page }) => {
    const active = page.locator("section.stage.active");

    // Precise tokens are disjoint: the tag is its own decoration, not a child of a speaker one.
    await expect(active.locator(".dd-tok-speaker-name .dd-tok-custom-tag")).toHaveCount(0);
    await expect(active.locator(".dd-tok-speaker-id .dd-tok-custom-tag")).toHaveCount(0);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveText("#happy");
});

test("a report with no semantic tokens shows no dialogue highlighting", async ({ page }) => {
    const plainUrl = writeReport({
        source: "Alice: Hi.\n",
        stages: SAMPLE_STAGES,
        semanticTokens: [],
    });
    await page.goto(plainUrl);
    await expect(page.locator(".tab")).toHaveCount(2);

    const active = page.locator("section.stage.active");
    await expect(active.locator(".dd-tok-speaker-name")).toHaveCount(0);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveCount(0);
});
