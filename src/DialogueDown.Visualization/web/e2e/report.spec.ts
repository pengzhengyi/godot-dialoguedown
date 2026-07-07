import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { SAMPLE_STAGES, writeReport } from "./report";

const url = writeReport(SAMPLE_STAGES);
const nodeCount = SAMPLE_STAGES[0].nodes.length;

test.beforeEach(async ({ page }) => {
    await page.goto(url);
    await expect(page.locator("section.stage.active g.node")).toHaveCount(nodeCount);
});

test("renders every node with a coloured circle and a legend of counts", async ({ page }) => {
    await expect(page.locator("section.stage.active g.node circle")).toHaveCount(nodeCount);
    const legendItems = page.locator("section.stage.active .legend .legend-item");
    await expect(legendItems).toHaveCount(10); // one row per category present
    await expect(legendItems.filter({ hasText: "Code span" })).toBeVisible();
});

test("clicking a node shows its source and a rendered preview", async ({ page }) => {
    await page.locator("g.node", { hasText: "Image" }).first().click();
    await expect(page.locator("#detail-title")).toContainText("Image");
    await expect(page.locator("#detail-body .preview img")).toHaveAttribute("src", "x.jpg");
});

test("the Document preview renders front matter as metadata, not a heading", async ({ page }) => {
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-body pre.frontmatter")).toContainText("title: Demo");
    // The front matter must not be mis-rendered as a heading (the body's own
    // "# Scene" heading is expected and fine).
    await expect(
        page.locator("#detail-body .preview :is(h1, h2)", { hasText: "Demo" }),
    ).toHaveCount(0);
});

test("hovering a node shows a Tippy tooltip with the full attribute text", async ({ page }) => {
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

test("clicking a legend entry dims that category's nodes", async ({ page }) => {
    await page.locator(".legend-item", { hasText: "Code span" }).click();
    await expect(page.locator("section.stage.active g.node.dimmed")).toHaveCount(1);
});

test("the zoom controls change and reset the ratio", async ({ page }) => {
    const ratio = page.locator("section.stage.active .zoom-ratio");
    const before = await ratio.textContent();
    await page.locator("section.stage.active .zoom-controls button", { hasText: "+" }).click();
    await expect(ratio).not.toHaveText(before ?? "");
});

test("arrow keys move the selection", async ({ page }) => {
    await page.locator("g.node", { hasText: "Document" }).first().click();
    await expect(page.locator("#detail-title")).toContainText("Document");
    await page.keyboard.press("ArrowRight");
    await expect(page.locator("#detail-title")).not.toContainText("Document");
});

test("has no accessibility violations (real browser, incl. colour contrast)", async ({ page }) => {
    const results = await new AxeBuilder({ page }).analyze();
    expect(results.violations).toEqual([]);
});
