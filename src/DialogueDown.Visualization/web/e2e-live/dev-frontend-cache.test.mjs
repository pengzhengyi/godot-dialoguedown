import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(here, "..");
const repositoryRoot = resolve(webRoot, "../../..");
const packageJson = JSON.parse(readFileSync(resolve(webRoot, "package.json"), "utf8"));
const gitignore = readFileSync(resolve(webRoot, ".gitignore"), "utf8");
const tasks = JSON.parse(readFileSync(resolve(repositoryRoot, ".vscode/tasks.json"), "utf8")).tasks;

test("TypeScript stores incremental state only in the ignored frontend cache", () => {
    assert.match(packageJson.scripts.typecheck, /--incremental/);
    assert.match(
        packageJson.scripts.typecheck,
        /--tsBuildInfoFile \.cache\/typescript\/tsconfig\.tsbuildinfo/,
    );
    assert.match(gitignore, /^\.cache\/$/m);

    const clean = tasks.find((task) => task.label === "clean");
    assert.ok(clean);
    assert.match(clean.command, /web\/\.cache/);
});

test("ESLint stores its native cache under the ignored frontend cache", () => {
    assert.match(packageJson.scripts["lint:js"], /--cache/);
    assert.match(packageJson.scripts["lint:js"], /--cache-location \.cache\/eslint\//);
    assert.match(gitignore, /^\.cache\/$/m);
});

test("Stylelint stores its native cache under the ignored frontend cache", () => {
    assert.match(packageJson.scripts["lint:css"], /--cache/);
    assert.match(packageJson.scripts["lint:css"], /--cache-location \.cache\/stylelint\//);
    assert.match(gitignore, /^\.cache\/$/m);
});
