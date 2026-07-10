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

test("serves a live report bound to the document", async ({ page }) => {
    // The payload is marked live, so the tabs render and the Source tab is present.
    await expect(page.locator(".tab")).toHaveCount(3);
    await expect(page.locator(".tab").first()).toHaveText("Source");
    // The mode badge reflects watch mode.
    await expect(page.locator("#mode-badge")).toHaveText("Hot Reload");
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
