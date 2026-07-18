import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import { LIVE_DOC, LIVE_PORT, INITIAL_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the live e2e. Writes a fresh temp document (so the
// server has something to watch before it binds), then runs the real .NET live
// server against it on the fixed loopback port. Playwright waits for the URL to
// respond, then runs the specs; on teardown it terminates this process tree.
writeFileSync(LIVE_DOC, INITIAL_SOURCE);

spawnCli([
    "visualize",
    LIVE_DOC,
    "--root",
    dirname(LIVE_DOC),
    "--port",
    String(LIVE_PORT),
    "--no-open",
]);
