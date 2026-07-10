import { test, expect } from "@playwright/test";
import { readFileSync, writeFileSync } from "node:fs";
import { LIVE_EDIT_PORT, LIVE_EDIT_DOC, LIVE_EDIT_SOURCE } from "./fixture.mjs";

// Live Edit end-to-end against the real .NET --live server: the Source tab is an
// editable CodeMirror buffer, edits update the preview as you type, Save (Ctrl/Cmd+S)
// writes the file, and an external change surfaces a passive chip without reloading.
const base = `http://127.0.0.1:${LIVE_EDIT_PORT}`;

test.beforeEach(() => {
    writeFileSync(LIVE_EDIT_DOC, LIVE_EDIT_SOURCE);
});

test("edits update the preview and mark the tab dirty; Save writes the file", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await page.locator(".cm-content").click();
    await page.keyboard.press("End");
    await page.keyboard.type(" and a game call `Wave()`");

    // Preview updates as you type and the Source tab goes dirty, but the file is untouched.
    await expect(page.locator(".source-preview")).toContainText("Wave()");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).not.toContain("Wave()");

    // Save writes the buffer to disk and clears the dirty marker.
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("Wave()");
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
