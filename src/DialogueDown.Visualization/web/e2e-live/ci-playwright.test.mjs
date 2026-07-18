import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(here, "../../../..");

test("CI installs only the Chromium headless shell", () => {
    const workflow = readFileSync(resolve(repositoryRoot, ".github/workflows/ci.yml"), "utf8");

    assert.match(workflow, /npx playwright install --with-deps --only-shell chromium/);
});

test("Playwright configurations do not request a headed browser", () => {
    for (const config of ["playwright.config.ts", "playwright.live.config.ts"]) {
        const source = readFileSync(resolve(here, "..", config), "utf8");
        assert.doesNotMatch(source, /headless\s*:\s*false/);
    }
});
