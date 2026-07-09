import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./highlight.css";
import "./styles.css";

import { runApp } from "./app";
import { startLiveClient } from "./live-client";
import { DEV_SOURCE, DEV_STAGES } from "./dev-stages";
import type { Report } from "./model";

/**
 * The .NET library replaces the `"__REPORT__"` slot in report.html with the
 * report JSON, so `window.__DD_REPORT__` is an object at runtime. During local
 * development the placeholder is left as-is and a sample is shown instead.
 */
function resolveReport(): Report {
    const raw = (window as unknown as { __DD_REPORT__?: unknown }).__DD_REPORT__;
    if (raw && typeof raw === "object" && Array.isArray((raw as Report).stages)) {
        return raw as Report;
    }
    if (import.meta.env.DEV) return { source: DEV_SOURCE, stages: DEV_STAGES };
    return { stages: [] };
}

const report = resolveReport();
const app = runApp(report);
// When served by the live server, subscribe for hot-reload pushes.
if (report.live) startLiveClient(app);
