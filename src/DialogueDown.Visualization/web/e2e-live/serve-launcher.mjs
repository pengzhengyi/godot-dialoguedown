import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    LAUNCHER_TREE,
    LAUNCHER_TOP_DOC,
    LAUNCHER_SUB_DOC,
    LAUNCHER_TOP_SOURCE,
    LAUNCHER_SUB_SOURCE,
    LAUNCHER_PORT,
} from "./fixture.mjs";

// The Playwright webServer for the launcher e2e. Builds a small tree — a script at
// the root and one in a sub-folder — then runs the real .NET launcher (visualize
// with a root but no source, so it opens the launcher) on the fixed loopback port.
// Playwright waits for the URL to respond, runs the specs, and terminates this
// process tree on teardown.
mkdirSync(dirname(LAUNCHER_SUB_DOC), { recursive: true });
writeFileSync(LAUNCHER_TOP_DOC, LAUNCHER_TOP_SOURCE);
writeFileSync(LAUNCHER_SUB_DOC, LAUNCHER_SUB_SOURCE);

spawnCli(["visualize", "--root", LAUNCHER_TREE, "--port", String(LAUNCHER_PORT), "--no-open"]);
