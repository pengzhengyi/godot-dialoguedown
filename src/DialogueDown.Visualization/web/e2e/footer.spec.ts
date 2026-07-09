import { test, expect } from "@playwright/test";
import type { Report } from "../src/model";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// A deliberately long absolute path so the document path fills the status bar and
// forces the layout to cope on a narrow screen.
const LONG_PATH =
    "/Users/pengzhengyi/Documents/Dev/PersonalProjects/Game/DialogueDown/very/deep/nested/a-really-quite-long-scene-file-name.dialogue.md";

const REPORT_WITH_PATH: Report = {
    mode: "watch",
    path: LONG_PATH,
    source: SAMPLE_SOURCE,
    stages: SAMPLE_STAGES,
};

// Light scheme is the harder case for the historic bug: the button-inverse text
// colour (white) would be invisible on the light footer.
test.use({ colorScheme: "light" });

test("the document path stays legible on hover (not white-on-white)", async ({ page }) => {
    await page.goto(writeReport(REPORT_WITH_PATH));
    const path = page.locator("#doc-path");
    await expect(path).toBeVisible();

    await path.hover();

    const color = await path.evaluate((el) => getComputedStyle(el).color);
    const [r, g, b] = color.match(/\d+/g)!.map(Number);
    const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    // The bug rendered the hover text as the button inverse (white, luminance 255),
    // which vanished on the light footer. Require a clearly darker foreground.
    expect(luminance).toBeLessThan(180);
});

test("on a narrow screen the help wraps below the status bar, not over the path", async ({
    page,
}) => {
    await page.setViewportSize({ width: 360, height: 720 });
    await page.goto(writeReport(REPORT_WITH_PATH));

    // Expand the contextual help — its widest state.
    await page.locator("#help-summary").click();

    const pathBox = (await page.locator("#doc-path").boundingBox())!;
    const helpBox = (await page.locator(".app-footer .help").boundingBox())!;

    const overlaps =
        pathBox.x < helpBox.x + helpBox.width &&
        pathBox.x + pathBox.width > helpBox.x &&
        pathBox.y < helpBox.y + helpBox.height &&
        pathBox.y + pathBox.height > helpBox.y;
    expect(overlaps).toBe(false);
    // The help sits on its own line, below the status bar.
    expect(helpBox.y).toBeGreaterThan(pathBox.y + 2);
});
