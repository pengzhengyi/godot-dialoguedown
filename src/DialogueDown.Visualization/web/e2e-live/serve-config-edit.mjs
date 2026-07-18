import { mkdirSync, writeFileSync } from "node:fs";
import { spawnCli } from "./cli-runner.mjs";
import {
    CONFIG_EDIT_TREE,
    CONFIG_EDIT_DOC,
    CONFIG_EDIT_TOML,
    CONFIG_EDIT_PORT,
    CONFIG_EDIT_SOURCE,
    CONFIG_EDIT_CONFIG,
} from "./fixture.mjs";

// The Playwright webServer for the config-edit e2e: lay out a script beside a fresh
// `dialogue.toml`, then run the real .NET server in --edit mode so the Config tab is an
// editable TOML editor whose Save recompiles the configured speakers.
mkdirSync(CONFIG_EDIT_TREE, { recursive: true });
writeFileSync(CONFIG_EDIT_DOC, CONFIG_EDIT_SOURCE);
writeFileSync(CONFIG_EDIT_TOML, CONFIG_EDIT_CONFIG);

spawnCli([
    "visualize",
    CONFIG_EDIT_DOC,
    "--edit",
    "--root",
    CONFIG_EDIT_TREE,
    "--port",
    String(CONFIG_EDIT_PORT),
    "--no-open",
]);
