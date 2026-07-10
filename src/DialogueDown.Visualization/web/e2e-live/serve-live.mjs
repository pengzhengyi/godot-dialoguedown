import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { LIVE_EDIT_DOC, LIVE_EDIT_PORT, LIVE_EDIT_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the Live Edit e2e: write a fresh temp document, then run
// the real .NET server in --live (editable) mode against it on the fixed loopback port.
writeFileSync(LIVE_EDIT_DOC, LIVE_EDIT_SOURCE);

const server = spawn(
    "dotnet",
    [
        "run",
        "--project",
        "../../DialogueDown.Cli",
        "-c",
        "Release",
        "--",
        "visualize",
        LIVE_EDIT_DOC,
        "--live",
        "--root",
        dirname(LIVE_EDIT_DOC),
        "--port",
        String(LIVE_EDIT_PORT),
        "--no-open",
    ],
    { stdio: "inherit" },
);

const stop = () => server.kill("SIGTERM");
process.on("SIGTERM", stop);
process.on("SIGINT", stop);
server.on("exit", (code) => process.exit(code ?? 0));
