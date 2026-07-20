import { test, expect } from "@playwright/test";
import { SAMPLE_STAGES, writeReport } from "./report";
import type { Report } from "../src/model";

// A duplicate-anchor error (DLG2001) on the second "# Chapter" heading (zero-based line 3).
const REPORT: Report = {
    source: "# Chapter\nAlice: Hello.\n\n# Chapter\nBob: Goodbye.\n",
    stages: SAMPLE_STAGES,
    diagnostics: [
        {
            range: { start: { line: 3, character: 0 }, end: { line: 3, character: 9 } },
            severity: 1,
            code: "DLG2001",
            message: "Two scenes resolve to the same anchor '#chapter'.",
            source: "dialoguedown",
        },
    ],
};

const url = writeReport(REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

test("renders a squiggle and a gutter marker for a diagnostic", async ({ page }) => {
    const active = page.locator("section.stage.active");
    await expect(active.locator(".cm-lintRange-error").first()).toBeVisible();
    await expect(active.locator(".cm-gutter-lint .cm-lint-marker-error")).toHaveCount(1);
});

test("hovering a diagnostic shows its message and a docs link", async ({ page }) => {
    const active = page.locator("section.stage.active");
    await active.locator(".cm-lintRange-error").first().hover();

    const tooltip = page.locator(".cm-tooltip-lint");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("Two scenes resolve to the same anchor '#chapter'.");

    const link = tooltip.locator("a.diagnostic-tooltip-link");
    await expect(link).toHaveAttribute("href", /guide\/error-codes\.html#dlg2001$/);
    await expect(link).toContainText("DLG2001");
});

test("a report with no diagnostics shows no overlay", async ({ page }) => {
    const cleanUrl = writeReport({ source: "# Scene\nAlice: Hi.\n", stages: SAMPLE_STAGES });
    await page.goto(cleanUrl);
    await expect(page.locator(".tab")).toHaveCount(2);

    const active = page.locator("section.stage.active");
    await expect(active.locator(".cm-lintRange-error")).toHaveCount(0);
    await expect(active.locator(".cm-lint-marker-error")).toHaveCount(0);
});
