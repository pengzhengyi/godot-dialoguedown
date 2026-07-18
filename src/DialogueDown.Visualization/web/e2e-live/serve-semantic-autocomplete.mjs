import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    SEMANTIC_AUTOCOMPLETE_DOC,
    SEMANTIC_AUTOCOMPLETE_PORT,
    SEMANTIC_AUTOCOMPLETE_SOURCE,
} from "./fixture.mjs";

writeFileSync(SEMANTIC_AUTOCOMPLETE_DOC, SEMANTIC_AUTOCOMPLETE_SOURCE);

spawnCli([
    "visualize",
    SEMANTIC_AUTOCOMPLETE_DOC,
    "--edit",
    "--root",
    dirname(SEMANTIC_AUTOCOMPLETE_DOC),
    "--port",
    String(SEMANTIC_AUTOCOMPLETE_PORT),
    "--no-open",
]);
