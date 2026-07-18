import { writeFileSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    RENDER_ROOT_TREE,
    RENDER_ROOT_DOC,
    RENDER_ROOT_IMAGE,
    RENDER_ROOT_SOURCE,
    RENDER_ROOT_PORT,
} from "./fixture.mjs";

// A second Playwright webServer that exercises `--render-root`. It lays out a tree
// where the document references an image outside its own folder, then serves the
// common ancestor explicitly (no consent prompt) so the report — served at the
// document's sub-path — can resolve the `../` image link.
const PNG_1x1 = Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "base64",
);

mkdirSync(dirname(RENDER_ROOT_DOC), { recursive: true });
mkdirSync(dirname(RENDER_ROOT_IMAGE), { recursive: true });
writeFileSync(RENDER_ROOT_IMAGE, PNG_1x1);
writeFileSync(RENDER_ROOT_DOC, RENDER_ROOT_SOURCE);

spawnCli([
    "visualize",
    RENDER_ROOT_DOC,
    "--port",
    String(RENDER_ROOT_PORT),
    "--root",
    RENDER_ROOT_TREE,
    "--no-open",
]);
