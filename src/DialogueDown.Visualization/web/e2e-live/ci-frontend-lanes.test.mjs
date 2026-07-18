import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const workflow = readFileSync(resolve(here, "../../../../.github/workflows/ci.yml"), "utf8");

function job(id, nextId) {
    const start = workflow.indexOf(`  ${id}:`);
    const end = nextId ? workflow.indexOf(`  ${nextId}:`, start + 1) : workflow.length;
    assert.notEqual(start, -1, `missing ${id} job`);
    assert.notEqual(end, -1, `missing ${nextId} job after ${id}`);
    return workflow.slice(start, end);
}

test("frontend verification is split into independent quality and E2E lanes", () => {
    const quality = job("frontend_quality", "frontend_static");
    assert.match(quality, /name: Frontend quality/);
    assert.match(quality, /run: npm run check/);
    assert.match(quality, /run: npm run build/);
    assert.match(quality, /git diff --exit-code/);
    assert.doesNotMatch(quality, /playwright install/);

    const staticE2e = job("frontend_static", "frontend_live");
    assert.match(staticE2e, /name: Frontend static E2E/);
    assert.match(staticE2e, /playwright install --with-deps --only-shell chromium/);
    assert.match(staticE2e, /run: npm run e2e/);
    assert.doesNotMatch(staticE2e, /setup-dotnet/);

    const liveE2e = job("frontend_live", "frontend");
    assert.match(liveE2e, /name: Frontend live E2E/);
    assert.match(liveE2e, /actions\/setup-dotnet/);
    assert.match(liveE2e, /playwright install --with-deps --only-shell chromium/);
    assert.match(liveE2e, /run: npm run e2e:live/);
});

test("the stable Frontend check aggregates every frontend lane", () => {
    const aggregate = job("frontend");
    assert.match(aggregate, /name: Frontend/);
    assert.match(aggregate, /needs: \[frontend_quality, frontend_static, frontend_live\]/);
    assert.match(aggregate, /needs\.frontend_quality\.result/);
    assert.match(aggregate, /needs\.frontend_static\.result/);
    assert.match(aggregate, /needs\.frontend_live\.result/);
});
