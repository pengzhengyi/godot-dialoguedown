import { defineConfig, devices } from "@playwright/test";
import { LIVE_PORT, RENDER_ROOT_PORT, LAUNCHER_PORT } from "./e2e-live/fixture.mjs";

// Live e2e: exercises the real .NET live server end-to-end in a browser — hot
// reload, the missing-document banner, and consent-gated asset hosting. Kept
// separate from the fast static `file://` suite (playwright.config.ts) because it
// starts servers.
const baseURL = `http://127.0.0.1:${LIVE_PORT}`;

export default defineConfig({
    testDir: "./e2e-live",
    testMatch: "**/*.spec.ts",
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    reporter: "list",
    use: { baseURL },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
    webServer: [
        {
            command: "node ./e2e-live/serve.mjs",
            url: baseURL,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-renderroot.mjs",
            url: `http://127.0.0.1:${RENDER_ROOT_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-launcher.mjs",
            url: `http://127.0.0.1:${LAUNCHER_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
    ],
});
