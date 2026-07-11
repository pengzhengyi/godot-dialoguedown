import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_REPORT, writeReport } from "./report";

const url = writeReport(SAMPLE_REPORT);

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

/** Switch to the Markdown AST graph tab and wait for it to render. */
async function showAst(page: Page): Promise<void> {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
}

test("the Source preview collapses to give the editor the full width, and restores", async ({
    page,
}) => {
    const view = page.locator("section.stage.active.source-stage .source-view");
    const preview = view.locator(".source-preview");
    const toggle = view.locator(".collapse-toggle");

    await expect(preview).toBeVisible();
    await expect(toggle).toHaveAttribute("aria-expanded", "true");

    await toggle.click();
    await expect(view).toHaveClass(/preview-collapsed/);
    await expect(preview).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");

    await toggle.click();
    await expect(view).not.toHaveClass(/preview-collapsed/);
    await expect(preview).toBeVisible();
});

test("the graph inspector collapses to give the graph the full width, and restores", async ({
    page,
}) => {
    await showAst(page);
    const app = page.locator("#app");
    const detail = page.locator("#detail");
    const toggle = page.locator("#resizer .collapse-toggle");

    await expect(detail).toBeVisible();
    await expect(toggle).toHaveAttribute("aria-expanded", "true");

    await toggle.click();
    await expect(app).toHaveClass(/detail-collapsed/);
    await expect(detail).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");

    await toggle.click();
    await expect(app).not.toHaveClass(/detail-collapsed/);
    await expect(detail).toBeVisible();
});

test("the collapsed state is remembered across a reload", async ({ page }) => {
    await showAst(page);
    await page.locator("#resizer .collapse-toggle").click();
    await expect(page.locator("#app")).toHaveClass(/detail-collapsed/);

    await page.reload();
    await showAst(page);
    // The inspector stays collapsed after reload (persisted in localStorage).
    await expect(page.locator("#app")).toHaveClass(/detail-collapsed/);
    await expect(page.locator("#detail")).toBeHidden();
});
