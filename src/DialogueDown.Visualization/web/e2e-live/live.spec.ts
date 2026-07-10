import { test, expect } from "@playwright/test";
import { writeFileSync, rmSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { LIVE_DOC, INITIAL_SOURCE } from "./fixture.mjs";

// A 1×1 PNG, written next to the document so a relative image link can resolve.
const PNG_1x1 = Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "base64",
);

// These tests share one server and document (the config runs them serially).
// Each test restores the document to its initial content first and waits for the
// page to reflect it, so tests are order-independent.
test.beforeEach(async ({ page }) => {
    writeFileSync(LIVE_DOC, INITIAL_SOURCE);
    await page.goto("/");
    await expect(page.locator(".source-pane .cm-content")).toContainText("Original Scene");
});

test("serves a view report bound to the document", async ({ page }) => {
    // The payload is a served session, so the tabs render and the Source tab is present.
    await expect(page.locator(".tab")).toHaveCount(3);
    await expect(page.locator(".tab").first()).toHaveText("Source");
    // The View/Edit toggle is shown, starting in View.
    await expect(page.locator('.mode-toggle-option[data-mode="view"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
    // The document path is shown in the status bar.
    await expect(page.locator("#doc-path")).toBeVisible();
});

test("serves images alongside the document so relative links resolve", async ({ page }) => {
    const assets = join(dirname(LIVE_DOC), "assets");
    mkdirSync(assets, { recursive: true });
    writeFileSync(join(assets, "pic.png"), PNG_1x1);
    writeFileSync(LIVE_DOC, "# Gallery\n\n![a picture](assets/pic.png)\n");

    const image = page.locator(".source-preview img");
    await expect(image).toBeVisible();
    // The image actually loaded (served by the live server), not a broken link.
    await expect
        .poll(async () => image.evaluate((img: HTMLImageElement) => img.naturalWidth))
        .toBeGreaterThan(0);

    rmSync(assets, { recursive: true, force: true });
});

test("hot-reloads the report when the document changes on disk", async ({ page }) => {
    writeFileSync(LIVE_DOC, "# Rewritten Scene\n\nBob: A brand new line.\n");

    // The server watches the file, recompiles, and pushes over SSE; the client
    // rebuilds in place. No reload/navigation here — the DOM updates itself.
    await expect(page.locator(".source-pane .cm-content")).toContainText("Rewritten Scene");
    await expect(page.locator(".source-preview")).toContainText("A brand new line");
});

test("keeps the active tab across a hot reload", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active")).not.toHaveClass(/source-stage/);

    writeFileSync(LIVE_DOC, "# Another Scene\n\nAlice: Still on the graph tab.\n");

    // After the rebuild the Markdown AST tab is still the active one.
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator("section.stage.active g.node")).not.toHaveCount(0);
});

test("shows a banner when the document is deleted", async ({ page }) => {
    await expect(page.locator("#live-banner")).toBeHidden();

    rmSync(LIVE_DOC);

    await expect(page.locator("#live-banner")).toBeVisible();
    await expect(page.locator("#live-banner")).toContainText("not found");
});

test("toggles to Edit and back to View, reconfiguring the one editor in place", async ({
    page,
}) => {
    const view = page.locator('.mode-toggle-option[data-mode="view"]');
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');

    // Starts in View: read-only (edits are rejected) and no Save button.
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
    await page.locator(".cm-content").click();
    await page.keyboard.type("XX");
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("XX");

    // Switch to Edit: editable and the Save button appears; the buffer is unchanged.
    await edit.click();
    await expect(edit).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeVisible();
    await page.locator(".cm-content").click();
    await page.keyboard.type("YY");
    await expect(page.locator(".source-pane .cm-content")).toContainText("YY"); // edits land now

    // Switch back to View (accepting the discard prompt) — read-only again.
    page.once("dialog", (dialog) => void dialog.accept());
    await view.click();
    await expect(view).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator(".save-button")).toBeHidden();
});

test("accents blue in View and green in Edit, and keeps the footer on one aligned line", async ({
    page,
}) => {
    const edit = page.locator('.mode-toggle-option[data-mode="edit"]');
    const activeTab = page.locator("button.tab.active");
    const GREEN = "rgb(21, 128, 61)"; // #15803d, the Save-button green

    // View: the document reports view mode and the accent is not the Edit green.
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "view");
    await expect(activeTab).not.toHaveCSS("border-bottom-color", GREEN);

    // Edit: the accent switches to green on the active tab and the pressed toggle.
    await edit.click();
    await expect(page.locator("html")).toHaveAttribute("data-served-mode", "edit");
    await expect(activeTab).toHaveCSS("border-bottom-color", GREEN);
    await expect(edit).toHaveCSS("background-color", GREEN);

    // The status line keeps the toggle, path, and help toggle vertically centered together.
    const centers = await page.evaluate(() => {
        const center = (selector: string): number => {
            const rect = document.querySelector(selector)!.getBoundingClientRect();
            return rect.top + rect.height / 2;
        };
        return {
            toggle: center(".mode-toggle"),
            path: center("#doc-path"),
            help: center("#help-toggle"),
        };
    });
    expect(Math.abs(centers.toggle - centers.path)).toBeLessThan(2);
    expect(Math.abs(centers.toggle - centers.help)).toBeLessThan(2);
});
