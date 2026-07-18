import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const workflow = readFileSync(resolve(here, "../../../../.github/workflows/ci.yml"), "utf8");

test("CI cancels only stale runs for the same pull request or ref", () => {
    assert.match(
        workflow,
        /group: ci-\$\{\{ github\.workflow \}\}-\$\{\{ github\.event\.pull_request\.number \|\| github\.ref \}\}/,
    );
    assert.match(workflow, /cancel-in-progress: true/);
});
