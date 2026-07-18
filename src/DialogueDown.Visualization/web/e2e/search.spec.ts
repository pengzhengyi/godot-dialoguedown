import { test, expect } from "@playwright/test";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// SAMPLE_SOURCE has a `# Scene` heading and speakers `Alice` / `Bob`, so a search finds matches.
const url = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES, mode: "edit" });

test("the compact find panel opens on the shortcut, counts, toggles, and closes", async ({
    page,
}) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+f");

    const panel = page.locator(".dd-search");
    await expect(panel).toBeVisible();

    // A query reports its match count.
    await panel.locator(".dd-search-field .dd-search-input").fill("Scene");
    await expect(panel.locator(".dd-search-count")).toContainText("result");

    // The case toggle reflects its pressed state.
    const caseToggle = panel.locator(".dd-search-toggle").first();
    await caseToggle.click();
    await expect(caseToggle).toHaveAttribute("aria-pressed", "true");

    // The replace input expands on the chevron.
    await panel.locator(".dd-search-expand").click();
    await expect(panel.locator(".dd-search-replace-input")).toBeVisible();

    // Escape dismisses the panel.
    await page.keyboard.press("Escape");
    await expect(panel).toHaveCount(0);
});
