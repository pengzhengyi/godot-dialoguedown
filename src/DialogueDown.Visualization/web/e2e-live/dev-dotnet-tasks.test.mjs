import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const tasks = JSON.parse(
    readFileSync(resolve(here, "../../../../.vscode/tasks.json"), "utf8"),
).tasks;

test("the fast .NET build skips analyzers without replacing the full build", () => {
    const fast = tasks.find((task) => task.label === "build: fast");
    const full = tasks.find((task) => task.label === "build");

    assert.ok(fast);
    assert.match(fast.command, /RunAnalyzers=false/);
    assert.match(fast.command, /--no-restore/);
    assert.notDeepEqual(fast.group, { kind: "build", isDefault: true });

    assert.ok(full);
    assert.doesNotMatch(full.command, /RunAnalyzers=false/);
    assert.deepEqual(full.group, { kind: "build", isDefault: true });
});

test("targeted .NET tasks select a project and optional filter", () => {
    const project = tasks.find((task) => task.label === "test: project");
    const filtered = tasks.find((task) => task.label === "test: filter");

    assert.ok(project);
    assert.match(project.command, /\$\{input:dotnetTestProject\}/);
    assert.match(project.command, /--no-build/);
    assert.match(project.command, /--no-restore/);

    assert.ok(filtered);
    assert.match(filtered.command, /\$\{input:dotnetTestProject\}/);
    assert.match(filtered.command, /\$\{input:dotnetTestFilter\}/);
    assert.match(filtered.command, /--filter/);
});
