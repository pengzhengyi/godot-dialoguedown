import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

// Shared knobs for the live e2e: the temp document the server watches and the
// fixed loopback port. `serve.mjs` (the Playwright webServer) and the spec both
// derive the document path from this directory so they always agree.
const here = dirname(fileURLToPath(import.meta.url));

export const LIVE_PORT = 5177;
export const LIVE_DOC = join(here, ".live-doc.dialogue.md");
export const INITIAL_SOURCE = "# Original Scene\n\nAlice: The original line.\n";

// A second live server for the --render-root path: the document sits in a
// sub-folder and references an image in a sibling folder (outside its own), so the
// server must host the common ancestor and serve the report at the sub-path.
export const RENDER_ROOT_PORT = 5178;
export const RENDER_ROOT_TREE = join(here, ".render-root");
export const RENDER_ROOT_DOC = join(RENDER_ROOT_TREE, "proj", "scene.dialogue.md");
export const RENDER_ROOT_IMAGE = join(RENDER_ROOT_TREE, "shared", "out.png");
export const RENDER_ROOT_SOURCE = "# Gallery\n\n![an outside picture](../shared/out.png)\n";

// A launcher server over a small tree: a script at the root and one in a
// sub-folder, so the e2e can browse the tree, descend into the folder, and open
// either script's report.
export const LAUNCHER_PORT = 5179;
export const LAUNCHER_TREE = join(here, ".launcher-tree");
export const LAUNCHER_TOP_DOC = join(LAUNCHER_TREE, "top.dialogue.md");
export const LAUNCHER_SUB_DOC = join(LAUNCHER_TREE, "sub", "nested.dialogue.md");
export const LAUNCHER_TOP_SOURCE = "# Top Scene\n\nAlice: The script at the root.\n";
export const LAUNCHER_SUB_SOURCE = "# Nested Scene\n\nBob: The script in a sub-folder.\n";
