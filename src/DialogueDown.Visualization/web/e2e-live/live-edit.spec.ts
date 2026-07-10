import { test, expect, type Page } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { LIVE_EDIT_PORT, LIVE_EDIT_DOC, LIVE_EDIT_SOURCE } from "./fixture.mjs";

// Live Edit end-to-end against the real .NET --live server: the Source tab is an
// editable CodeMirror buffer, edits update the preview as you type, the Save button and
// the Ctrl/⌘+S shortcut write the file from any tab, and an external change surfaces a
// passive chip without a reload.
const base = `http://127.0.0.1:${LIVE_EDIT_PORT}`;

test.beforeEach(() => {
    writeFileSync(LIVE_EDIT_DOC, LIVE_EDIT_SOURCE);
});

async function edit(page: Page, text: string) {
    await page.locator(".cm-content").click();
    await page.keyboard.press("End");
    await page.keyboard.type(text);
}

test("edits update the preview and dirty state; the Save button writes the file", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Nothing to save yet: the Save button is present but disabled.
    const save = page.locator(".save-button");
    await expect(save).toBeVisible();
    await expect(save).toBeDisabled();

    await edit(page, " and a game call `Wave()`");

    // Preview updates as you type; the tab goes dirty and Save enables; the file is untouched.
    await expect(page.locator(".source-preview")).toContainText("Wave()");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(save).toBeEnabled();
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).not.toContain("Wave()");

    // Clicking Save writes the buffer, clears dirty, and disables the button again.
    await save.click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(save).toBeDisabled();
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("Wave()");
});

test("the Ctrl/Cmd+S shortcut saves from a graph tab, not just the Source tab", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await edit(page, " via a shortcut");

    // Switch away from the Source tab to a graph tab, then press the shortcut there.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await page.keyboard.press("ControlOrMeta+s");

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via a shortcut");
});

test("the Save button also saves from a graph tab", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await edit(page, " via the button");

    // The Save button lives in the status bar, so it is reachable from every tab.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    const save = page.locator(".save-button");
    await expect(save).toBeEnabled();
    await save.click();

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via the button");
});

test("an external change surfaces the passive chip without discarding local edits", async ({
    page,
}) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Make a local edit, then change the file on disk from outside the editor.
    await page.locator(".cm-content").click();
    await page.keyboard.type("X");
    writeFileSync(LIVE_EDIT_DOC, "# Rewritten from disk\n");

    // The chip appears; the live editor is not reloaded, so the local buffer survives.
    await expect(page.locator(".disk-chip")).toBeVisible();
    await expect(page.locator(".cm-content")).toContainText("Alice");
});
