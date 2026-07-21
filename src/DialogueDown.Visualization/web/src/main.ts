import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "./styles.css";

import { runApp } from "./app";
import { watchServerEvents } from "./live-client";
import { createLiveEdit, type LiveEditController } from "./live-edit";
import { initLiveEditUi, type DocumentBinding } from "./live-edit-ui";
import { createSaveModeStore } from "./save-mode";
import { createModeToggle } from "./mode-toggle";
import { createModeController } from "./view-edit";
import { createConfig, browserConfigCreatePorts } from "./config-create";
import { resolveDocumentForNavigation } from "./navigation";
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
    const sourceStore = createSaveModeStore("source");
    const configStore = createSaveModeStore("config");
    // Only the latest navigation intent runs; a later navigation or a mode change bumps the token
    // so a superseded flush never replays a stale transition.
    let navToken = 0;
    // runApp activates a tab during construction (before `ui` and the controllers exist), so the
    // active-document reflection is armed only once everything below is wired.
    let controllersReady = false;

    // The active document's controller: the config when the Config tab is active, else the
    // dialogue (node-inspector edits share the Source controller). A Config tab with no config
    // file has no controller, so there is no active save controller (shared controls disable).
    const activeLive = (): LiveEditController | null =>
        app.isConfigTabActive() ? configLive : dialogueLive;

    // Resolve one document before navigation: Auto flushes and awaits the latest generation;
    // Manual awaits the current save, then prompts save-or-discard. A paused
    // conflict/uncertain/waiting/error stays in place, so navigation is never an implicit retry.
    function resolveDocument(
        live: LiveEditController,
        isCancelled: () => boolean = () => false,
    ): Promise<boolean> {
        return resolveDocumentForNavigation(
            live,
            () =>
                window.confirm(
                    "You have unsaved changes. Discard them to continue? " +
                        "Click Cancel to keep editing, then Save.",
                ),
            isCancelled,
        );
    }

    // The async navigation boundary for tabs and node selection: resolve the active document,
    // then run `proceed` — but only when a newer navigation has not superseded this one. A Config
    // tab with no controller has nothing to resolve, so navigation proceeds.
    function beginNavigation(proceed: () => void): void {
        const token = ++navToken;
        const live = activeLive();
        if (live === null) {
            proceed();
            return;
        }
        // A newer navigation (or a mode change) bumps navToken; pass that as the cancellation
        // signal so the Auto flush loop stops rather than saving on behalf of a superseded intent.
        void resolveDocument(live, () => token !== navToken).then((ok) => {
            if (ok && token === navToken) proceed();
        });
    }

    const app = runApp(report, {
        editable: initialMode === "edit",
        onChange: (buffer) => controller.onEditorChange(buffer),
        configOnChange: (buffer) => controller.onConfigEditorChange(buffer),
        onCreateConfig: () => createConfig(browserConfigCreatePorts()),
        beginNavigation,
        onActiveTabChange: () => {
            if (controllersReady) ui.reflectActiveDocument();
        },
        symbols: createSemanticSymbolSource(() => currentSymbols),
    });
    const ui = initLiveEditUi(app, { active: () => activeLive() });
    const dialogueBinding: DocumentBinding = {
        type: "source",
        markDirty: app.markSourceDirty,
        setContent: app.setContent,
        applyReport: (applied) => {
            app.updateStages(applied.stages);
            // A save recompiles, so the analyzer's symbols change — refresh the completion
            // holder or the editor keeps offering the old speakers/ids.
            currentSymbols = applied.symbols;
            app.setDiagnostics(applied.diagnostics ?? []);
        },
        diskSource: (applied) => applied.source ?? "",
    };
    const dialogueLive = createLiveEdit(
        ui.portsFor(dialogueBinding),
        {
            documentType: "source",
            mode: sourceStore.get(),
            onModeChange: (m) => {
                sourceStore.set(m);
                navToken += 1; // a mode change clears any pending navigation
            },
        },
        report.source ?? "",
    );
    const configBinding: DocumentBinding = {
        type: "config",
        target: "config",
        markDirty: app.markConfigDirty,
        setContent: (source) => app.setConfigContent(source),
        applyReport: (applied) => {
            if (applied.configuration) app.updateConfig(applied.configuration);
            // A config recompile changes the graph too, so refresh the stages exactly once, like
            // the Source binding does.
            app.updateStages(applied.stages);
            // Editing the config changes the resolved speakers/ids, so refresh the Source
            // editor's completion symbols and diagnostics from the same recompile.
            currentSymbols = applied.symbols;
            app.setDiagnostics(applied.diagnostics ?? []);
        },
        diskSource: (applied) => applied.configuration?.file?.source ?? "",
        // The speakers pane is stale whenever the buffer's config is not the compiled report.
        onStatus: (status) => app.setConfigStale(status !== "saved"),
    };
    const configInitiallyInvalid = report.configStatus === "saved-invalid";
    const configLive: LiveEditController | null = report.configuration?.file
        ? createLiveEdit(
              ui.portsFor(configBinding),
              {
                  documentType: "config",
                  mode: configStore.get(),
                  initialValid: !configInitiallyInvalid,
                  onModeChange: (m) => {
                      configStore.set(m);
                      navToken += 1;
                  },
              },
              report.configuration.file.source,
          )
        : null;
    // A page that loaded with a persisted-but-invalid Config starts with a stale report: the
    // speakers pane reflects the last valid compile, not the invalid buffer now in the editor.
    if (configInitiallyInvalid) app.setConfigStale(true);
    controllersReady = true;
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
        resolveDocument,
    });
    document.getElementById("mode-badge")?.replaceWith(toggle.element);
    watchServerEvents({
        onReload: (next) => {
            currentSymbols = next.symbols;
            controller.onReload(next);
        },
        onReloadConfig: (next) => {
            currentSymbols = next.symbols;
            controller.onReloadConfig(next);
        },
        onProblem: (message, target) => controller.onProblem(message, target),
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
