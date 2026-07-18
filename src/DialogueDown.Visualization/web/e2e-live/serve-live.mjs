import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import { LIVE_EDIT_DOC, LIVE_EDIT_PORT, LIVE_EDIT_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the Live Edit e2e: write a fresh temp document, then run
// the real .NET server in --edit (editable) mode against it on the fixed loopback port.
writeFileSync(LIVE_EDIT_DOC, LIVE_EDIT_SOURCE);

spawnCli([
    "visualize",
    LIVE_EDIT_DOC,
    "--edit",
    "--root",
    dirname(LIVE_EDIT_DOC),
    "--port",
    String(LIVE_EDIT_PORT),
    "--no-open",
]);
