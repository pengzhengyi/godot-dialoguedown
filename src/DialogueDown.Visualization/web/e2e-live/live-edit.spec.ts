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
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("End");
    await page.keyboard.type(text);
}

/** Select a graph node whose tooltip contains `tip` on the active tab (SVG needs dispatch). */
async function selectNode(page: Page, tip: string) {
    await page.evaluate((tip) => {
        const node = [...document.querySelectorAll("section.stage.active svg .node")].find((n) =>
            (n.getAttribute("data-tip") ?? "").includes(tip),
        );
        (node?.querySelector("circle") ?? node)?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
    }, tip);
}

/** Type into the node inspector's editor (distinct from the Source tab's editor). */
async function editNode(page: Page, text: string) {
    const content = page.locator(".node-source .cm-content");
    await content.click();
    await page.keyboard.press("End");
    // insertText (not type): each node keystroke re-splices and re-renders the preview, so
    // synthetic char-by-char typing can outrun it and drop spaces; a real user never does.
    await page.keyboard.insertText(text);
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
    await expect(page.locator(".source-pane .cm-content")).toContainText("unwanted trailing note");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Accepting it restores the editor to the last saved version and clears dirty.
    page.once("dialog", (dialog) => void dialog.accept());
    await discard.click();
    await expect(page.locator(".source-pane .cm-content")).not.toContainText(
        "unwanted trailing note",
    );
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

    await expect(page.locator(".source-pane .cm-content")).toContainText("FIRST");
    await expect(page.locator(".source-pane .cm-content")).not.toContainText("SECOND");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
});

test("the Ctrl/Cmd+S shortcut saves a node edit from a graph tab", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    // Edit a node directly on a graph tab, then save with the shortcut from there.
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " via a shortcut");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    await page.keyboard.press("ControlOrMeta+s");

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via a shortcut");
});

test("the Save button saves a node edit from a graph tab", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " via the button");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // The Save button lives in the status bar, so it is reachable from every tab.
    const save = page.locator(".save-button");
    await expect(save).toBeEnabled();
    await save.click();

    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("via the button");
});

test("formatting shortcuts and emphasis auto-surround wrap the selection", async ({ page }) => {
    await page.goto(`${base}/`);
    await expect(page.locator(".source-pane .cm-editor")).toBeVisible();

    const content = page.locator(".source-pane .cm-content");
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
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.type("X");
    writeFileSync(LIVE_EDIT_DOC, "# Rewritten from disk\n");

    // The chip appears; the live editor is not reloaded, so the local buffer survives.
    await expect(page.locator(".disk-chip")).toBeVisible();
    await expect(page.locator(".source-pane .cm-content")).toContainText("Alice");
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
        };
    });
}

const scrollBy = (page: Page, selector: string, total: number, step: number) =>
    page.evaluate(
        async ([selector, total, step]) => {
            const el = document.querySelector<HTMLElement>(selector as string)!;
            for (let done = 0; done < (total as number); done += step as number) {
                el.scrollTop += step as number;
                // Give each step a couple of frames so the follower (and CodeMirror's
                // re-measure of newly rendered lines) keeps up before the next nudge.
                await new Promise((r) => setTimeout(r, 60));
            }
        },
        [selector, total, step] as const,
    );

test("the editor and preview scroll in sync, anchored on scenes", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, SCROLL_DOC);
    // The editor virtualizes its lines, so assert on the preview (full HTML) to know the
    // long document loaded. Reused between the two directions for a clean, unowned slate.
    const load = async () => {
        await expect(async () => {
            await page.goto(`${base}/`);
            await expect(page.locator(".source-preview")).toContainText(`Scene ${SCENES}`, {
                timeout: 2000,
            });
        }).toPass({ timeout: 20_000 });
        await expect(page.locator(".source-pane .cm-editor")).toBeVisible();
    };
    const scrollTopOf = (selector: string) =>
        page.evaluate((s) => document.querySelector<HTMLElement>(s)!.scrollTop, selector);

    // Scrolling the editor down carries the preview to the same scene. The panes have
    // different heights, so this is a real mapping (not a shared scrollbar), and the exact
    // within-scene pixel offset depends on the platform's line metrics — so the robust
    // invariant is that the follower moved off the top and shows the same scene.
    await load();
    await scrollBy(page, ".source-pane .cm-scroller", 1600, 150);
    await page.waitForTimeout(300);
    const byEditor = await headingsAtTop(page);
    expect(await scrollTopOf(".source-preview")).toBeGreaterThan(100); // the preview followed
    expect(byEditor.editorScene).toBe(byEditor.previewScene);

    // Reload for a clean slate (neither pane owns the sync), then drive from the preview:
    // scrolling it down carries the editor to the same scene (bidirectional).
    await load();
    await scrollBy(page, ".source-preview", 1600, 150);
    await page.waitForTimeout(300);
    const byPreview = await headingsAtTop(page);
    expect(await scrollTopOf(".source-pane .cm-scroller")).toBeGreaterThan(100); // the editor followed
    expect(byPreview.editorScene).toBe(byPreview.previewScene);
});

// A multi-node document with a speaker-less line, so the Dialogue AST has a synthetic
// (filled default speaker) node to prove is read-only.
const NODE_DOC = "# Market\n\nGuide: Welcome.\n\nA line with no speaker.\n";

test("edits a node's source in the inspector, and Save recompiles from it", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // Select a Text node: its source shows in the inspector editor, editable in Edit.
    await selectNode(page, "Text");
    const nodeEditor = page.locator(".node-source .cm-editor");
    await expect(nodeEditor).toBeVisible();

    // Editing the node re-renders the inspector preview as you type (before Save).
    await editNode(page, " EDITED");
    await expect(page.locator(".node-source .source-preview")).toContainText("EDITED");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Save splices the edit into the document, writes the file, and recompiles the graphs.
    await page.keyboard.press("ControlOrMeta+s");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
    await expect.poll(() => readFileSync(LIVE_EDIT_DOC, "utf8")).toContain("EDITED");
});

// The inspector editor must swallow graph-navigation keys so arrows move the cursor, not
// the graph, and Space types a space instead of collapsing a node. The global keydown
// handler otherwise routes those keys to the active tree view. Cover both the read-only
// editor (View) and the editable one (Edit), a fresh page each so neither run's editor
// focus bleeds into the other.
for (const mode of ["view", "edit"] as const) {
    test(`editor keys stay in the editor and never move the graph selection (${mode})`, async ({
        page,
    }) => {
        writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
        await page.goto(`${base}/`);
        if (mode === "edit") {
            await page.locator('.mode-toggle-option[data-mode="edit"]').click();
        }
        await page.locator(".tab", { hasText: "Dialogue AST" }).click();
        await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

        // Select a node so the graph has a selection that arrow keys could move.
        await selectNode(page, "Text");
        const selected = page.locator("section.stage.active g.node.selected");
        await expect(selected).toHaveCount(1);
        const before = await selected.getAttribute("data-tip");
        const collapsedBefore = await page.locator("section.stage.active g.node.collapsed").count();

        // Press the very keys the tree view navigates by, from inside the editor.
        await page.locator(".node-source .cm-content").click();
        for (const key of ["ArrowRight", "ArrowLeft", "ArrowDown", "ArrowUp", "Space"]) {
            await page.keyboard.press(key);
        }

        // The selection and every node's collapse state are exactly as before.
        await expect(selected).toHaveCount(1);
        expect(await selected.getAttribute("data-tip")).toBe(before);
        expect(await page.locator("section.stage.active g.node.collapsed").count()).toBe(
            collapsedBefore,
        );
    });
}

test("a synthetic node offers no editor, only an inserted note", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    // The default speaker is inserted by desugar, so it appears on the Desugared AST tab.
    await page.locator(".tab", { hasText: "Desugared AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();

    // The speaker-less line's filled default speaker is synthetic: no source to edit. The
    // note explains why and points the reader at the editable parent line instead.
    await selectNode(page, "default");
    const detailNote = page.locator("#detail-body .node-note");
    await expect(detailNote).toContainText("names no speaker");
    await expect(detailNote).toContainText("Edit the line to name one");
});

test("navigation locks while a node edit is unsaved", async ({ page }) => {
    writeFileSync(LIVE_EDIT_DOC, NODE_DOC);
    await page.goto(`${base}/`);
    await page.locator(".tab", { hasText: "Dialogue AST" }).click();
    await expect(page.locator("section.stage.active g.node").first()).toBeVisible();
    await selectNode(page, "Text");
    await editNode(page, " X");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Cancelling the prompt keeps you on the tab with your edit intact.
    page.once("dialog", (d) => void d.dismiss());
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Dialogue AST");
    await expect(page.locator(".tab.dirty")).toHaveCount(1);

    // Accepting the prompt discards the edit and lets navigation proceed.
    page.once("dialog", (d) => void d.accept());
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator(".tab.active")).toHaveText("Markdown AST");
    await expect(page.locator(".tab.dirty")).toHaveCount(0);
});
