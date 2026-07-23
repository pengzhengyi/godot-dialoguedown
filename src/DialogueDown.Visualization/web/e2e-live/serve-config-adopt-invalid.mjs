import { mkdirSync, rmSync, writeFileSync } from "node:fs";
import { spawnCli } from "./cli-runner.mjs";
import {
    CONFIG_ADOPT_INVALID_TREE,
    CONFIG_ADOPT_INVALID_DOC,
    CONFIG_ADOPT_INVALID_TOML,
    CONFIG_ADOPT_INVALID_PORT,
    CONFIG_ADOPT_INVALID_SOURCE,
} from "./fixture.mjs";

// The Playwright webServer for the config-adopt-invalid e2e: lay out a script with NO
// `dialogue.toml` in an isolated tree, then run the real .NET server in --edit mode, so the session
// starts config-less. The spec drops an *invalid* pre-existing dialogue.toml on disk before
// clicking Create, exercising the adopt-existing-invalid recovery path (saved-invalid).
mkdirSync(CONFIG_ADOPT_INVALID_TREE, { recursive: true });
rmSync(CONFIG_ADOPT_INVALID_TOML, { force: true });
writeFileSync(CONFIG_ADOPT_INVALID_DOC, CONFIG_ADOPT_INVALID_SOURCE);

spawnCli([
    "visualize",
    CONFIG_ADOPT_INVALID_DOC,
    "--edit",
    "--root",
    CONFIG_ADOPT_INVALID_TREE,
    "--port",
    String(CONFIG_ADOPT_INVALID_PORT),
    "--no-open",
]);
