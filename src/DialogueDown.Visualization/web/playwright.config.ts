import { defineConfig, devices } from "@playwright/test";

// e2e tests load the actual built report (dist/index.html) via page.setContent
// after injecting sample stage data into the __STAGES__ slot, so no web server is
// needed — the report is fully self-contained.
export default defineConfig({
    testDir: "./e2e",
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    reporter: "list",
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
