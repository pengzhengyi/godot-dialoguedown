import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// An editable (Edit-mode) report so the Edit-only completions are active, and a static
// (read-only) one to prove they are absent there. SAMPLE_SOURCE has `# Scene` and
// `## The Market` headings and speakers `Alice` / `Bob`.
const editUrl = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES, mode: "edit" });
const staticUrl = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES });

// The semantic analyzer's resolved symbols, as the .NET payload carries them. These names
// are deliberately absent from SAMPLE_SOURCE (no `@id`, no `#tag`, no "Docks" heading) so a
// completion offering them can only have come from the payload, not the document scan.
const editWithSymbolsUrl = writeReport({
    source: SAMPLE_SOURCE,
    stages: SAMPLE_STAGES,
    mode: "edit",
    symbols: {
        jumpTargets: [{ slug: "the-docks", heading: "The Docks" }],
        speakers: ["Merchant"],
        speakerIds: ["merchant"],
        tags: ["wise"],
    },
});

/** Put the cursor at the end of the document, ready to type a fresh line. */
async function focusEditorEnd(page: Page): Promise<void> {
    await page.locator(".cm-content").click();
    await page.keyboard.press("ControlOrMeta+End");
}

const tooltip = ".cm-tooltip-autocomplete";

test("completes a jump target from the document's headings", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAlice: Go [x](#the-m");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["the-market"]);
});

test("completes a speaker id after @", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    // SAMPLE_SOURCE declares no @ids, so add one, then complete it on a later line.
    await page.keyboard.type("\nGuide @guide: Hi.\n@gu");
    await expect(page.locator(tooltip)).toBeVisible();
    const labels = await page.locator(`${tooltip} li`).allInnerTexts();
    expect(labels).toContain("guide");
    expect(labels).not.toContain("gu"); // the half-typed id does not suggest itself
});

test("completes a tag after a mid-line #", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAlice #happy: Hi.\nBob #ha");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["happy"]);
});

test("completes a known speaker at the start of a line", async ({ page }) => {
    await page.goto(editUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAli");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["Alice"]);
});

test("does not offer completions in the read-only static report", async ({ page }) => {
    await page.goto(staticUrl);
    await page.locator(".cm-content").click();
    // Even an explicit completion request surfaces nothing when the editor is read-only.
    await page.keyboard.press("ControlOrMeta+ ");
    await expect(page.locator(tooltip)).toHaveCount(0);
});

test("completes a resolved speaker id carried in the semantic payload", async ({ page }) => {
    await page.goto(editWithSymbolsUrl);
    await focusEditorEnd(page);
    // `merchant` is not typed anywhere in the document, so it can only come from the payload.
    await page.keyboard.type("\n@mer");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["merchant"]);
});

test("completes a resolved jump target carried in the semantic payload", async ({ page }) => {
    await page.goto(editWithSymbolsUrl);
    await focusEditorEnd(page);
    await page.keyboard.type("\nAlice: Go [x](#the-d");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["the-docks"]);
});

test("merges the payload with the live scan rather than replacing it", async ({ page }) => {
    await page.goto(editWithSymbolsUrl);
    await focusEditorEnd(page);
    // `Bob` is in the document but not in the payload, so completing it proves the scan half
    // still contributes alongside the resolved symbols.
    await page.keyboard.type("\nBo");
    await expect(page.locator(tooltip)).toBeVisible();
    await expect(page.locator(`${tooltip} li`)).toContainText(["Bob"]);
});
