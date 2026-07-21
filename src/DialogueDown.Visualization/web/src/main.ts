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
import { browserConfigCreatePorts, createConfig } from "./config-create";
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

    // The active document's controller: the config when the Config tab is active, else the
    // dialogue (node-inspector edits share the Source controller).
    const activeLive = (): LiveEditController =>
        configLive && app.isConfigTabActive() ? configLive : dialogueLive;

    // Resolve one document before navigation: Auto flushes and awaits; Manual prompts
    // save-or-discard. A paused conflict/uncertain/waiting/error stays in place, so navigation is
    // never an implicit retry.
    async function resolveDocument(live: LiveEditController): Promise<boolean> {
        const paused =
            live.status === "conflict" ||
            live.status === "uncertain" ||
            live.status === "waiting" ||
            live.status === "error";
        if (paused) return false;
        if (!live.dirty) return true;
        if (live.mode === "auto") {
            const outcome = await live.flush();
            return outcome === "saved" || outcome === "saved-invalid" || outcome === "noop";
        }
        const discard = window.confirm(
            "You have unsaved changes. Discard them to continue? " +
                "Click Cancel to keep editing, then Save.",
        );
        if (discard) live.discardChanges();
        return discard;
    }

    // The async navigation boundary for tabs and node selection: resolve the active document,
    // then run `proceed` — but only when a newer navigation has not superseded this one.
    function beginNavigation(proceed: () => void): void {
        const token = ++navToken;
        void resolveDocument(activeLive()).then((ok) => {
            if (ok && token === navToken) proceed();
        });
    }

    const app = runApp(report, {
        editable: initialMode === "edit",
        onChange: (buffer) => controller.onEditorChange(buffer),
        configOnChange: (buffer) => controller.onConfigEditorChange(buffer),
        onCreateConfig: () => createConfig(browserConfigCreatePorts()),
        beginNavigation,
        onActiveTabChange: () => ui.reflectActiveDocument(),
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
            // Editing the config changes the resolved speakers/ids, so refresh the Source
            // editor's completion symbols and diagnostics from the same recompile.
            currentSymbols = applied.symbols;
            app.setDiagnostics(applied.diagnostics ?? []);
        },
        diskSource: (applied) => applied.configuration?.file?.source ?? "",
        // The speakers pane is stale whenever the buffer's config is not the compiled report.
        onStatus: (status) => app.setConfigStale(status !== "saved"),
    };
    const configLive: LiveEditController | null = report.configuration?.file
        ? createLiveEdit(
              ui.portsFor(configBinding),
              {
                  documentType: "config",
                  mode: configStore.get(),
                  onModeChange: (m) => {
                      configStore.set(m);
                      navToken += 1;
                  },
              },
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
        resolveDocument,
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
