import { mkdirSync, rmSync, writeFileSync } from "node:fs";
import { spawnCli } from "./cli-runner.mjs";
import {
    CONFIG_CREATE_TREE,
    CONFIG_CREATE_DOC,
    CONFIG_CREATE_TOML,
    CONFIG_CREATE_PORT,
    CONFIG_CREATE_SOURCE,
} from "./fixture.mjs";

// The Playwright webServer for the config-create e2e: lay out a script with NO `dialogue.toml`
// in an isolated tree, then run the real .NET server in --edit mode. The Config tab shows the
// no-config state, so the e2e can click "Create dialogue.toml" and watch the reload land on the
// editable Config tab. A stale config from a previous run is cleared first (so the create route
// does not 409 on re-run).
mkdirSync(CONFIG_CREATE_TREE, { recursive: true });
rmSync(CONFIG_CREATE_TOML, { force: true });
writeFileSync(CONFIG_CREATE_DOC, CONFIG_CREATE_SOURCE);

spawnCli([
    "visualize",
    CONFIG_CREATE_DOC,
    "--edit",
    "--root",
    CONFIG_CREATE_TREE,
    "--port",
    String(CONFIG_CREATE_PORT),
    "--no-open",
]);
