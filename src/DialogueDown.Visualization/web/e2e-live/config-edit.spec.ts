import { test, expect } from "@playwright/test";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import {
    CONFIG_EDIT_PORT,
    CONFIG_EDIT_TREE,
    CONFIG_EDIT_DOC,
    CONFIG_EDIT_TOML,
    CONFIG_EDIT_SOURCE,
    CONFIG_EDIT_CONFIG,
} from "./fixture.mjs";

// Config Live Edit end-to-end against the real .NET --edit server: the Config tab's TOML is
// an editable buffer; editing marks it dirty and its speakers pane stale, and Save writes
// dialogue.toml and recompiles so the configured speakers refresh.
const base = `http://127.0.0.1:${CONFIG_EDIT_PORT}`;

const BOB = '\n[[speakers]]\nname = "Bob"\nid = "B"\n';

test.beforeEach(() => {
    mkdirSync(CONFIG_EDIT_TREE, { recursive: true });
    writeFileSync(CONFIG_EDIT_DOC, CONFIG_EDIT_SOURCE);
    writeFileSync(CONFIG_EDIT_TOML, CONFIG_EDIT_CONFIG);
});

/** Open the Config tab and append `text` at the end of the TOML editor. */
async function appendToConfig(page: import("@playwright/test").Page, text: string): Promise<void> {
    await page.locator(".tab", { hasText: "Config" }).click();
    await page.locator(".config-source .cm-content").click();
    await page.keyboard.press("ControlOrMeta+End"); // CodeMirror Mod-End = document end
    await page.keyboard.insertText(text); // insertText avoids the auto-close-bracket per-key path
}

test("editing the config marks it dirty and stale; Save recompiles the speakers", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();
    await expect(page.locator(".config-speakers-table")).toContainText("Alice");
    await expect(page.locator(".config-stale-hint")).toBeHidden();

    await appendToConfig(page, BOB);

    // The Config tab goes dirty, the speakers pane is signposted stale, and Save enables.
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(page.locator(".config-stale-hint")).toBeVisible();
    await expect(page.locator(".save-button")).toBeEnabled();

    await page.locator(".save-button").click();

    // The recompile refreshes the configured speakers and clears dirty + the stale hint.
    await expect(page.locator(".config-speakers-table")).toContainText("Bob");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(page.locator(".config-stale-hint")).toBeHidden();
    expect(readFileSync(CONFIG_EDIT_TOML, "utf8")).toContain("Bob");
});

test("navigation locks while the config is unsaved", async ({ page }) => {
    await page.goto(base);
    await appendToConfig(page, BOB);
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Declining the discard prompt keeps you on the Config tab with the edits intact.
    page.once("dialog", (dialog) => dialog.dismiss());
    await page.locator(".tab", { hasText: "Source" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Config");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
});
