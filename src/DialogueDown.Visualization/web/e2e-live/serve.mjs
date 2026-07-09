import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
import { LIVE_DOC, LIVE_PORT, INITIAL_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the live e2e. Writes a fresh temp document (so the
// server has something to watch before it binds), then runs the real .NET live
// server against it on the fixed loopback port. Playwright waits for the URL to
// respond, then runs the specs; on teardown it terminates this process tree.
writeFileSync(LIVE_DOC, INITIAL_SOURCE);

const server = spawn(
    "dotnet",
    [
        "run",
        "--project",
        "../../DialogueDown.Visualization.Live",
        "-c",
        "Release",
        "--",
        LIVE_DOC,
        "--watch",
        "--port",
        String(LIVE_PORT),
        "--no-open",
    ],
    { stdio: "inherit" },
);

const stop = () => server.kill("SIGTERM");
process.on("SIGTERM", stop);
process.on("SIGINT", stop);
server.on("exit", (code) => process.exit(code ?? 0));
