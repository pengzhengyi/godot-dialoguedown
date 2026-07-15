import { test, expect } from "@playwright/test";
import { rmSync } from "node:fs";
import { join } from "node:path";
import { LAUNCHER_PORT, LAUNCHER_TREE } from "./fixture.mjs";

// Targets the launcher server (visualize --root <tree>, no source): browse the
// tree, open a report under /r/, and return via Back to launcher. Runs against the
// real .NET launcher started by serve-launcher.mjs.
const base = `http://127.0.0.1:${LAUNCHER_PORT}`;

test.beforeEach(async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".launcher-title")).toHaveText("Open a script");
});

test("lists the root's scripts and folders", async ({ page }) => {
    await expect(page.locator(".launcher-item.src", { hasText: "top.dialogue.md" })).toBeVisible();
    await expect(page.locator(".launcher-item.dir", { hasText: "sub" })).toBeVisible();
});

test("opens a root script (static) and returns via Back to launcher", async ({ page }) => {
    await page.locator(".launcher-item.src", { hasText: "top.dialogue.md" }).click();
    await page.locator(".launcher-open").click();

    // The open POST answers 303 to the report under the /r/ mount.
    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".tab")).toHaveCount(5); // Source + Markdown/Dialogue/Desugared AST + Semantic Model
    await expect(page.locator(".tab").first()).toHaveText("Source");

    await page.locator("a.back-to-launcher").click();
    await expect(page.locator(".launcher-title")).toHaveText("Open a script");
});

test("browses into a sub-folder and opens a nested script in View", async ({ page }) => {
    await page.locator(".launcher-item.dir", { hasText: "sub" }).click();
    await expect(page.locator(".launcher-item.up")).toBeVisible(); // ".."
    const nested = page.locator(".launcher-item.src", { hasText: "nested.dialogue.md" });
    await expect(nested).toBeVisible();

    await nested.click();
    await page.locator('.mode-toggle-option[data-mode="view"]').click();
    await page.locator(".launcher-open").click();

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".tab")).toHaveCount(5);
});

// The create tests write into the launch tree; remove the file afterward so a rerun and the
// other tests see the base fixture. (The "exists" test opens an existing script, so nothing
// is created and force:true makes the removal a no-op.)
const createdInTest = join(LAUNCHER_TREE, "created-in-test.dialogue.md");
test.afterEach(() => rmSync(createdInTest, { force: true }));

test("creates a new script from the launcher and opens it in Edit", async ({ page }) => {
    await page.locator(".launcher-new").click();
    await page.locator(".launcher-create-name").fill("created-in-test");
    await page.locator(".launcher-create-submit").click();

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator('.mode-toggle-option[data-mode="edit"]')).toHaveAttribute(
        "aria-pressed",
        "true",
    );
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
});

test("an existing name offers to open it instead of overwriting", async ({ page }) => {
    page.once("dialog", (dialog) => void dialog.accept()); // "open it instead?"
    await page.locator(".launcher-new").click();
    await page.locator(".launcher-create-name").fill("top"); // top.dialogue.md already exists
    await page.locator(".launcher-create-submit").click();

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".source-pane .cm-editor")).toContainText("Top Scene");
});
