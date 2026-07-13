import { test, expect } from "@playwright/test";
import { LAUNCHER_PORT } from "./fixture.mjs";

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
    await page.locator('input[name="launcher-mode"][value="view"]').check();
    await page.locator(".launcher-open").click();

    await expect(page).toHaveURL(/\/r\//);
    await expect(page.locator(".tab")).toHaveCount(5);
});
