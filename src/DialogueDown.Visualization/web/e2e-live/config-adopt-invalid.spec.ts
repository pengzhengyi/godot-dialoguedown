import { test, expect } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import {
    CONFIG_ADOPT_INVALID_PORT,
    CONFIG_ADOPT_INVALID_TREE,
    CONFIG_ADOPT_INVALID_DOC,
    CONFIG_ADOPT_INVALID_TOML,
    CONFIG_ADOPT_INVALID_SOURCE,
    CONFIG_ADOPT_INVALID_CONFIG,
} from "./fixture.mjs";

// Config Create recovery end-to-end against the real .NET --edit server when the pre-existing
// dialogue.toml is *invalid*: the session starts config-less, but an invalid dialogue.toml already
// exists when the writer clicks "Create dialogue.toml". The server adopts it without overwriting as
// saved-invalid, and the reload must open an editable Config controller (not dead-end config-less)
// so the writer can fix and recover the file.
const base = `http://127.0.0.1:${CONFIG_ADOPT_INVALID_PORT}`;

test.beforeEach(() => {
    mkdirSync(CONFIG_ADOPT_INVALID_TREE, { recursive: true });
    writeFileSync(CONFIG_ADOPT_INVALID_DOC, CONFIG_ADOPT_INVALID_SOURCE);
    // An invalid hand-written config already on disk — the session was launched config-less, so it
    // does not yet apply it until the create-config request adopts it as saved-invalid.
    writeFileSync(CONFIG_ADOPT_INVALID_TOML, CONFIG_ADOPT_INVALID_CONFIG);
});

test("adopts an invalid existing dialogue.toml as saved-invalid so it can be edited", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();

    // The session is config-less, so the no-config create call to action is shown.
    await expect(page.locator(".config-empty-state")).toBeVisible();
    await page.locator(".config-create-button").click();

    // The page reloads onto the editable Config tab showing the *existing* invalid content in a
    // real Config controller (saved-invalid), so it never dead-ends config-less.
    await expect(page.locator(".tab.active", { hasText: "Config" })).toBeVisible();
    await expect(page.locator(".save-status[data-status='saved-invalid']")).toBeVisible();
    await expect(page.locator(".config-source .cm-content")).toContainText("bogus");
    // The existing invalid file is left byte-for-byte untouched on disk.
    expect(existsSync(CONFIG_ADOPT_INVALID_TOML)).toBe(true);
    expect(readFileSync(CONFIG_ADOPT_INVALID_TOML, "utf8")).toBe(CONFIG_ADOPT_INVALID_CONFIG);

    // The Config controller is live: replacing the invalid TOML with valid content and saving
    // recovers a valid configuration, so the writer is never stranded.
    await page.locator(".config-source .cm-content").click();
    await page.keyboard.press("ControlOrMeta+A");
    await page.keyboard.insertText('[[speakers]]\nname = "Alice"\nid = "A"\n');
    await expect(page.locator(".save-button")).toBeEnabled();
    await page.locator(".save-button").click();
    await expect(page.locator(".save-status[data-status='saved']")).toBeVisible();
});
