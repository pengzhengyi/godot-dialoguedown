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

test("the Discard button confirms, then restores the last saved version", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const save = page.locator(".save-button");
    const discard = page.locator(".discard-button");
    // Present but disabled until there are unsaved edits.
    await expect(discard).toBeVisible();
    await expect(discard).toBeDisabled();

    await edit(page, " and an unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    await expect(discard).toBeEnabled();

    // Cancelling the confirmation keeps the edits.
    page.once("dialog", (dialog) => void dialog.dismiss());
    await discard.click();
    await expect(page.locator(".cm-content")).toContainText("unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Accepting it restores the editor to the last saved version and clears dirty.
    page.once("dialog", (dialog) => void dialog.accept());
    await discard.click();
    await expect(page.locator(".cm-content")).not.toContainText("unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect(discard).toBeDisabled();
    await expect(save).toBeDisabled();
    // Discard never writes the file.
    expect(readFileSync(LIVE_EDIT_DOC, "utf8")).not.toContain("unwanted trailing note");
});

test("Discard after a save restores to the saved version, not the original", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Save one edit so it becomes the new baseline.
    await edit(page, " FIRST");
    await page.locator(".save-button").click();
    await expect(page.locator(".tab.dirty")).toHaveCount(0);

    // A second, unsaved edit, then Discard restores to the saved (FIRST) version.
    await edit(page, " SECOND");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);
    page.once("dialog", (dialog) => void dialog.accept());
    await page.locator(".discard-button").click();

    await expect(page.locator(".cm-content")).toContainText("FIRST");
    await expect(page.locator(".cm-content")).not.toContainText("SECOND");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
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

test("formatting shortcuts and emphasis auto-surround wrap the selection", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const content = page.locator(".cm-content");
    await content.click();

    // Bold via the ⌘/Ctrl+B shortcut wraps the selected word.
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("brave");
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.press("ControlOrMeta+b");
    await expect(content).toContainText("**brave**");

    // Emphasis auto-surround: typing an emphasis mark over a selection wraps it.
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("hero");
    await page.keyboard.press("ControlOrMeta+a");
    await page.keyboard.type("_");
    await expect(content).toContainText("_hero_");
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

// A long, multi-scene document so both panes actually scroll. Headings anchor the sync.
const SCENES = 8;
const SCROLL_DOC =
    "# Prologue\n\n" +
    Array.from({ length: SCENES }, (_, i) => {
        const body = Array.from(
            { length: 6 },
            (_, l) =>
                `Speaker${l % 3}: Line ${l} of scene ${i + 1}. Lorem ipsum dolor sit amet, ` +
                "consectetur adipiscing elit, plenty of filler so the line wraps.",
        ).join("\n\n");
        return `## Scene ${i + 1}\n\n${body}\n`;
    }).join("\n");

/** The heading nearest the top of each pane, and how far below the top it sits. */
async function headingsAtTop(page: Page) {
    return page.evaluate(() => {
        const scene = (label: string | null) => label?.replace(/^#+\s*/, "").trim() ?? null;
        const scroller = document.querySelector<HTMLElement>(".source-pane .cm-scroller")!;
        const preview = document.querySelector<HTMLElement>(".source-preview")!;
        const editorTop = scroller.getBoundingClientRect().top;
        const previewTop = preview.getBoundingClientRect().top;
        const nearestEditor = [...scroller.querySelectorAll(".cm-line")]
            .map((l) => ({
                d: l.getBoundingClientRect().top - editorTop,
                t: l.textContent!.trim(),
            }))
            .filter((o) => /^#{1,6}\s/.test(o.t))
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        const nearestPreview = [...preview.querySelectorAll("h1,h2,h3,h4,h5,h6")]
            .map((h) => ({
                d: h.getBoundingClientRect().top - previewTop,
                t: h.textContent!.trim(),
            }))
            .sort((a, b) => Math.abs(a.d) - Math.abs(b.d))[0];
        return {
            editorScene: scene(nearestEditor?.t ?? null),
            previewScene: scene(nearestPreview?.t ?? null),
            editorDelta: nearestEditor ? Math.round(nearestEditor.d) : null,
            previewDelta: nearestPreview ? Math.round(nearestPreview.d) : null,
        };
    });
}

const scrollBy = (page: Page, selector: string, total: number, step: number) =>
    page.evaluate(
        async ([selector, total, step]) => {
            const el = document.querySelector<HTMLElement>(selector as string)!;
            for (let done = 0; done < (total as number); done += step as number) {
                el.scrollTop += step as number;
                await new Promise((r) => setTimeout(r, 40)); // let each frame sync
            }
        },
        [selector, total, step] as const,
    );

test("the editor and preview scroll in sync, anchored on scenes", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, SCROLL_DOC);
    await expect(async () => {
        await page.goto(`${base}/`);
        // The editor virtualizes its lines, so assert on the preview (full HTML) to know
        // the long document loaded.
        await expect(page.locator(".source-preview")).toContainText(`Scene ${SCENES}`, {
            timeout: 2000,
        });
    }).toPass({ timeout: 20_000 });
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Scrolling the editor down brings the same scene to the top of the preview.
    await scrollBy(page, ".source-pane .cm-scroller", 1600, 150);
    await page.waitForTimeout(200);
    const afterEditor = await headingsAtTop(page);
    expect(afterEditor.editorScene).toBe(afterEditor.previewScene);
    expect(Math.abs(afterEditor.editorDelta! - afterEditor.previewDelta!)).toBeLessThan(40);

    // Scrolling the preview scrolls the editor back the same way (bidirectional).
    await scrollBy(page, ".source-preview", 1400, 140);
    await page.waitForTimeout(200);
    const afterPreview = await headingsAtTop(page);
    expect(afterPreview.editorScene).toBe(afterPreview.previewScene);
    expect(Math.abs(afterPreview.editorDelta! - afterPreview.previewDelta!)).toBeLessThan(40);
});
