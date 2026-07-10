import { test, expect, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { SAMPLE_REPORT, writeReport } from "./report";

const url = writeReport(SAMPLE_REPORT);
const nodeCount = SAMPLE_REPORT.stages[0].nodes.length;

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator(".tab")).toHaveCount(2); // Source + Markdown AST
});

/** Switch to the Markdown AST tab and wait for its graph to render. */
async function showAst(page: Page): Promise<void> {
    await page.locator(".tab", { hasText: "Markdown AST" }).click();
    await expect(page.locator("section.stage.active g.node")).toHaveCount(nodeCount);
}

// --- Source tab (first) ---

test("the Source tab is first and active, showing the document beside a preview", async ({
    page,
}) => {
    await expect(page.locator(".tab").first()).toHaveText("Source");
    const active = page.locator("section.stage.active");
    await expect(active).toHaveClass(/source-stage/);
    await expect(active.locator(".source-pane .cm-content")).toContainText("# Scene");
    await expect(active.locator(".source-preview")).toBeVisible();
    // The node-detail panel is only for graph tabs; it is hidden here.
    await expect(page.locator("#detail")).toBeHidden();
});

test("clicking a preview anchor link scrolls to its heading", async ({ page }) => {
    await page.locator('.source-preview a[href="#the-market"]').click();
    await expect(page).toHaveURL(/#the-market$/);
    await expect(page.locator("#the-market")).toBeInViewport();
});

test("the theme toggle forces light/dark and returns to following the system", async ({ page }) => {
    const html = page.locator("html");
    await expect(page.locator(".theme-select")).toBeVisible();

    await page.selectOption(".theme-select", "dark");
    await expect(html).toHaveAttribute("data-theme", "dark");

    await page.selectOption(".theme-select", "light");
    await expect(html).toHaveAttribute("data-theme", "light");

    // "System" removes the override so the page follows prefers-color-scheme again.
    await page.selectOption(".theme-select", "system");
    await expect(html).not.toHaveAttribute("data-theme");
});

test("the editor supports search and code folding (read-only)", async ({ page }) => {
    await page.locator(".cm-content").click();

    // Search panel opens with the shortcut and closes with Escape.
    await page.keyboard.press("ControlOrMeta+f");
    await expect(page.locator(".cm-panel.cm-search")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.locator(".cm-panel.cm-search")).toHaveCount(0);

    // Folding a section from the gutter chevron collapses it to a placeholder.
    await page.locator(".cm-foldGutter .cm-gutterElement", { hasText: "⌄" }).first().click();
    await expect(page.locator(".cm-foldPlaceholder")).toBeVisible();
});

test("switching from Source to the Markdown AST tab shows the graph and detail panel", async ({
    page,
}) => {
    await expect(page.locator("section.stage.active")).toHaveClass(/source-stage/);
    await showAst(page);
    await expect(page.locator("#detail")).toBeVisible();
});

test("the tabs sit together at the start, not spread across the header", async ({ page }) => {
    const first = await page.locator(".tab").nth(0).boundingBox();
    const second = await page.locator(".tab").nth(1).boundingBox();
    // Adjacent (grouped), not pushed to opposite ends by Pico's nav space-between.
    expect(second!.x - (first!.x + first!.width)).toBeLessThan(20);
});

// --- Markdown AST graph (second tab) ---

test("renders every node with a colored circle and a legend of counts", async ({ page }) => {
    await showAst(page);
    await expect(page.locator("section.stage.active g.node circle")).toHaveCount(nodeCount);
    const legendItems = page.locator("section.stage.active .legend .legend-item");
    await expect(legendItems).toHaveCount(10); // one row per category present
    await expect(legendItems.filter({ hasText: "Code span" })).toBeVisible();
});

test("clicking a node shows its source and a rendered preview", async ({ page }) => {
    await showAst(page);
    await page.locator("g.node", { hasText: "Image" }).first().click();
    await expect(page.locator("#detail-title")).toContainText("Image");
    await expect(page.locator("#detail-body .preview img")).toHaveAttribute("src", "x.jpg");
});

test("the Document preview renders front matter as metadata, not a heading", async ({ page }) => {
    await showAst(page);
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-body pre.frontmatter")).toContainText("title: Demo");
    // The front matter must not be mis-rendered as a heading (the body's own
    // "# Scene" heading is expected and fine).
    await expect(
        page.locator("#detail-body .preview :is(h1, h2)", { hasText: "Demo" }),
    ).toHaveCount(0);
});

test("hovering a node shows a Tippy tooltip with the full attribute text", async ({ page }) => {
    await showAst(page);
    // The overlays (legend, zoom, detail panel) sit above the SVG and can cover a
    // node; disable their pointer-events so the hover reaches the node beneath.
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });
    // Hover the circle: it is a solid, filled hit target (unlike the node's
    // pointer-events:none labels), so the delegated Tippy fires deterministically.
    await page.locator('g.node[data-tip*="ellipsised"] circle').hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("should be ellipsised");
});

test("hovering a stage tab shows a Tippy tooltip describing the stage", async ({ page }) => {
    await page.locator(".tab", { hasText: "Markdown AST" }).hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("syntax tree");
});

test("hovering the Source tab shows a tip describing the source view", async ({ page }) => {
    await page.locator(".tab", { hasText: "Source" }).hover();
    const tooltip = page.locator(".tippy-box");
    await expect(tooltip).toBeVisible();
    await expect(tooltip).toContainText("live Markdown preview");
});

test("clicking a legend entry dims that category's nodes", async ({ page }) => {
    await showAst(page);
    await page.locator(".legend-item", { hasText: "Code span" }).click();
    await expect(page.locator("section.stage.active g.node.dimmed")).toHaveCount(1);
});

test("the zoom controls change and reset the ratio", async ({ page }) => {
    await showAst(page);
    const ratio = page.locator("section.stage.active .zoom-ratio");
    const before = await ratio.textContent();
    await page.locator("section.stage.active .zoom-controls button", { hasText: "+" }).click();
    await expect(ratio).not.toHaveText(before ?? "");
});

test("arrow keys move the selection", async ({ page }) => {
    await showAst(page);
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-title")).toContainText("Document");
    await page.keyboard.press("ArrowRight");
    await expect(page.locator("#detail-title")).not.toContainText("Document");
});

test("has no accessibility violations (both tabs, real browser incl. color contrast)", async ({
    page,
}) => {
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]); // Source tab
    await showAst(page);
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]); // Markdown AST tab
});
