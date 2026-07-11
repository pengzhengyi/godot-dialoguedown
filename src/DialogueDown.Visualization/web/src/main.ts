import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./styles.css";

import { runApp } from "./app";
import { watchServerEvents } from "./live-client";
import { createLiveEdit } from "./live-edit";
import { initLiveEditUi } from "./live-edit-ui";
import { createModeToggle } from "./mode-toggle";
import { createModeController } from "./view-edit";
import { initModeBadge } from "./mode-badge";
import { initPathDisplay } from "./path-display";
import { initBackToLauncher } from "./back-link";
import { initTheme } from "./theme";
import { initHelpToggle } from "./help";
import { DEV_SOURCE, DEV_STAGES } from "./dev-stages";
import type { Report, ServedMode } from "./model";

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
    if (import.meta.env.DEV) return { source: DEV_SOURCE, stages: DEV_STAGES, mode: "edit" };
    return { stages: [] };
}

const report = resolveReport();
const header = document.querySelector<HTMLElement>(".app-header");

// Apply the saved color theme and mount the System/Light/Dark toggle (every mode).
initTheme(header?.querySelector(".header-controls") ?? null);

if (report.mode === "view" || report.mode === "edit") {
    // A served session: one editor toggled between View (read-only, auto-updating) and
    // Edit (editable, owns its buffer). The wiring closures reference `live`/`controller`
    // only when invoked (after they are created below).
    const initialMode: ServedMode = report.mode;
    const app = runApp(report, {
        editable: initialMode === "edit",
        onChange: (buffer) => controller.onEditorChange(buffer),
    });
    const ui = initLiveEditUi(app, () => void live.save());
    const live = createLiveEdit(ui, report.source ?? "");
    const toggle = createModeToggle(initialMode, (mode) => controller.switchTo(mode));
    const controller = createModeController(initialMode, {
        app,
        live,
        setSaveVisible: ui.setSaveVisible,
        reflect: (mode) => {
            // Drive the blue (View) / green (Edit) accent, then the toggle's pressed state.
            document.documentElement.dataset.servedMode = mode;
            toggle.reflect(mode);
        },
        confirmDiscard: () =>
            window.confirm("Discard unsaved edits and switch to View? Your changes will be lost."),
    });
    document.getElementById("mode-badge")?.replaceWith(toggle.element);
    watchServerEvents({
        onReload: (next) => controller.onReload(next),
        onProblem: (message) => app.showBanner(message),
    });
    if (header) initBackToLauncher(header, window.location.pathname);
} else {
    // Static export: read-only, no server, no toggle.
    runApp(report);
    initModeBadge("static");
    if (header) initBackToLauncher(header, window.location.pathname);
}

initPathDisplay(report.path);
initHelpToggle();
