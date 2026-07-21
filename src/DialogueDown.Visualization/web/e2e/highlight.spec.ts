import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Report } from "../src/model";

// A speaker with a custom tag on line 0, and a jump indicator on line 1. The speaker token
// spans the whole prefix "Alice #happy: " ([0, 14)); its "#happy" tag ([6, 12)) overlaps it.
const REPORT: Report = {
    source: "Alice #happy: Hi.\n=> #scene-2\n",
    stages: SAMPLE_STAGES,
    semanticTokens: [
        {
            range: { start: { line: 0, character: 0 }, end: { line: 0, character: 14 } },
            kind: "Speaker",
        },
        {
            range: { start: { line: 0, character: 6 }, end: { line: 0, character: 12 } },
            kind: "CustomTag",
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

test("colors the speaker, its tag, and the jump indicator distinctly", async ({ page }) => {
    const active = page.locator("section.stage.active");

    await expect(active.locator(".dd-tok-speaker").first()).toContainText("Alice");
    await expect(active.locator(".dd-tok-custom-tag")).toHaveText("#happy");
    await expect(active.locator(".dd-tok-jump")).toHaveText("=>");

    // The three kinds resolve to three different colors (the theme's token variables).
    const colors = await active.evaluate((root) => {
        const colorOf = (selector: string) =>
            getComputedStyle(root.querySelector(selector) as Element).color;
        return {
            speaker: colorOf(".dd-tok-speaker"),
            tag: colorOf(".dd-tok-custom-tag"),
            jump: colorOf(".dd-tok-jump"),
        };
    });
    expect(new Set([colors.speaker, colors.tag, colors.jump]).size).toBe(3);
});

test("nests the overlapping tag inside the coarse speaker so the tag paints on top", async ({
    page,
}) => {
    const active = page.locator("section.stage.active");

    // The tag decoration is a descendant of the speaker decoration (the compiler's coarse
    // speaker span covers the whole prefix; the editor layers the tag over it by nesting).
    await expect(active.locator(".dd-tok-speaker .dd-tok-custom-tag")).toHaveText("#happy");
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
    await expect(active.locator(".dd-tok-speaker")).toHaveCount(0);
    await expect(active.locator(".dd-tok-custom-tag")).toHaveCount(0);
});
