import { mkdirSync, rmSync, writeFileSync } from "node:fs";
import { spawnCli } from "./cli-runner.mjs";
import {
    CONFIG_ADOPT_TREE,
    CONFIG_ADOPT_DOC,
    CONFIG_ADOPT_TOML,
    CONFIG_ADOPT_PORT,
    CONFIG_ADOPT_SOURCE,
} from "./fixture.mjs";

// The Playwright webServer for the config-adopt e2e: lay out a script with NO `dialogue.toml` in an
// isolated tree, then run the real .NET server in --edit mode, so the session starts config-less
// and the Config tab shows the no-config state. The spec then drops a *different* pre-existing
// dialogue.toml on disk before clicking Create, exercising the adopt-existing recovery path.
mkdirSync(CONFIG_ADOPT_TREE, { recursive: true });
rmSync(CONFIG_ADOPT_TOML, { force: true });
writeFileSync(CONFIG_ADOPT_DOC, CONFIG_ADOPT_SOURCE);

spawnCli([
    "visualize",
    CONFIG_ADOPT_DOC,
    "--edit",
    "--root",
    CONFIG_ADOPT_TREE,
    "--port",
    String(CONFIG_ADOPT_PORT),
    "--no-open",
]);
