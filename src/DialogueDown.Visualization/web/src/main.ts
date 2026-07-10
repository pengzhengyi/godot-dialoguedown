import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./highlight.css";
import "./styles.css";

import { runApp } from "./app";
import { startLiveClient } from "./live-client";
import { createLiveEdit } from "./live-edit";
import { initLiveEditUi, watchDiskChanges } from "./live-edit-ui";
import { initModeBadge } from "./mode-badge";
import { initPathDisplay } from "./path-display";
import { initBackToLauncher } from "./back-link";
import { initTheme } from "./theme";
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
const header = document.querySelector<HTMLElement>(".app-header");

// Apply the saved color theme and mount the System/Light/Dark toggle (every mode).
initTheme(header?.querySelector(".header-controls") ?? null);

if (report.mode === "live") {
    // Live Edit: the editor owns the buffer. Save writes it and recompiles the graphs;
    // the disk stream only raises a passive chip, never a reload over your edits. The
    // wiring closures reference `live` only when invoked (after it is created below).
    const app = runApp(report, {
        onEdit: (buffer) => live.onEdit(buffer),
    });
    const live = createLiveEdit(
        initLiveEditUi(app, () => void live.save()),
        report.source ?? "",
    );
    watchDiskChanges(() => live.onDiskChange());
    if (header) initBackToLauncher(header, window.location.pathname);
} else {
    const app = runApp(report);
    if (header) initBackToLauncher(header, window.location.pathname);
    // Watch hot-reloads from the server; static is inert.
    if (report.mode === "watch") startLiveClient(app);
}

initModeBadge(report.mode ?? "static");
initPathDisplay(report.path);
