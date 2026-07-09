import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

// Shared knobs for the live e2e: the temp document the server watches and the
// fixed loopback port. `serve.mjs` (the Playwright webServer) and the spec both
// derive the document path from this directory so they always agree.
const here = dirname(fileURLToPath(import.meta.url));

export const LIVE_PORT = 5177;
export const LIVE_DOC = join(here, ".live-doc.dialogue.md");
export const INITIAL_SOURCE = "# Original Scene\n\nAlice: The original line.\n";
