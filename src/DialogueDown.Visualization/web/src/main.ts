import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./styles.css";

import { runApp } from "./app";
import { DEV_STAGES } from "./dev-stages";
import type { Stage } from "./model";

/**
 * The .NET library replaces the `"__STAGES__"` slot in index.html with the
 * report's stage JSON, so `window.__DD_STAGES__` is an array at runtime. During
 * local development the placeholder is left as-is and a sample is shown instead.
 */
function resolveStages(): Stage[] {
    const raw = (window as unknown as { __DD_STAGES__?: unknown }).__DD_STAGES__;
    if (Array.isArray(raw)) return raw as Stage[];
    if (import.meta.env.DEV) return DEV_STAGES;
    return [];
}

runApp(resolveStages());
