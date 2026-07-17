import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./styles.css";

import { runApp } from "./app";
import { watchServerEvents } from "./live-client";
import { createLiveEdit, type LiveEditController } from "./live-edit";
import { initLiveEditUi } from "./live-edit-ui";
import { createModeToggle } from "./mode-toggle";
import { createModeController } from "./view-edit";
import { initModeBadge } from "./mode-badge";
import { initPathDisplay, initConfigPath } from "./path-display";
import { initBackToLauncher } from "./back-link";
import { initTheme } from "./theme";
import { initHelpToggle } from "./help";
import { createSemanticSymbolSource } from "./semantic-symbols";
import { DEV_SOURCE, DEV_STAGES } from "./dev-stages";
import type { DialogueSymbols } from "./dialogue-symbols";
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
    // A served session: two editable inputs to one dialogue compile — the dialogue source
    // (Source tab) and the config `dialogue.toml` (Config tab) — each read-only in View and
    // editable in Edit. The wiring closures reference the controllers only when invoked
    // (after they are created below).
    const initialMode: ServedMode = report.mode;
    const toggle = createModeToggle(initialMode, (mode) => controller.switchTo(mode));
    // The semantic analyzer's resolved symbols, refreshed on each hot-reload. The editor's
    // completion source reads this holder every call, so a reload updates completions in place.
    let currentSymbols: DialogueSymbols | undefined = report.symbols;
    // Navigation (switching tabs or selecting another node) is locked while a document has
    // unsaved edits, so a stale graph is never shown beside them. At most one document is dirty
    // at a time (the nav-lock guarantees it); resolve it here: discard to continue, or cancel to
    // keep editing (then Save).
    const guardNavigation = (): boolean => {
        const dirtyLive = configLive?.dirty ? configLive : dialogueLive.dirty ? dialogueLive : null;
        if (!dirtyLive) return true;
        const discard = window.confirm(
            "You have unsaved changes. Discard them to continue? " +
                "Click Cancel to keep editing, then Save.",
        );
        if (discard) dirtyLive.discardChanges();
        return discard;
    };
    const app = runApp(report, {
        editable: initialMode === "edit",
        onChange: (buffer) => controller.onEditorChange(buffer),
        configOnChange: (buffer) => controller.onConfigEditorChange(buffer),
        confirmNavigation: guardNavigation,
        symbols: createSemanticSymbolSource(() => currentSymbols),
    });
    // Save (⌘S or the button) acts on the active document: the config when the Config tab is
    // active, else the dialogue. Since only one document can be dirty at a time, this always
    // targets the dirty one (or is a no-op).
    const activeLive = (): LiveEditController =>
        configLive && app.isConfigTabActive() ? configLive : dialogueLive;
    const ui = initLiveEditUi(
        app,
        () => void activeLive().save(),
        () => activeLive().discardChanges(),
    );
    const dialogueLive = createLiveEdit(
        ui.portsFor({ markDirty: app.markSourceDirty, setContent: app.setContent }),
        report.source ?? "",
    );
    const configLive: LiveEditController | null = report.configuration?.file
        ? createLiveEdit(
              ui.portsFor({
                  target: "config",
                  // A dirty config also marks its speakers pane stale until the next Save.
                  markDirty: (dirty) => {
                      app.markConfigDirty(dirty);
                      app.setConfigStale(dirty);
                  },
                  setContent: (source) => app.setConfigContent(source),
                  onSaved: (saved) => {
                      if (saved.configuration) app.updateConfigSpeakers(saved.configuration);
                  },
              }),
              report.configuration.file.source,
          )
        : null;
    const controller = createModeController(initialMode, {
        app,
        dialogueLive,
        configLive,
        setEditControlsVisible: ui.setEditControlsVisible,
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
        onReload: (next) => {
            currentSymbols = next.symbols;
            controller.onReload(next);
        },
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
initConfigPath(report.configuration);
initHelpToggle();
