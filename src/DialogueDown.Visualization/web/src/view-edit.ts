import type { AppController } from "./app";
import type { LiveEditController } from "./live-edit";
import type { Report, ServedMode } from "./model";

/** The side-effecting collaborators the mode controller drives (injected for testing). */
export interface ModeControllerPorts {
    /** Reconfigures the editors (editable/read-only, content) and the graph tabs. */
    app: AppController;
    /** The dialogue document's Edit-mode dirty/save state machine. */
    dialogueLive: LiveEditController;
    /** The config document's state machine, when the compile applied a `dialogue.toml`. */
    configLive: LiveEditController | null;
    /** Shows or hides the Save and Discard buttons (Edit only). */
    setEditControlsVisible(visible: boolean): void;
    /** Reflects the active mode on the toggle control. */
    reflect(mode: ServedMode): void;
    /** Asks before discarding unsaved edits when leaving Edit; returns true to proceed. */
    confirmDiscard(): boolean;
}

/** Drives a served session's View ⇄ Edit lifecycle. */
export interface ModeController {
    /** The active mode. */
    readonly mode: ServedMode;
    /** Route a Source-editor change: dirty tracking in Edit, ignored in View (a reload). */
    onEditorChange(buffer: string): void;
    /** Route a config-editor change: dirty tracking in Edit, ignored in View. */
    onConfigEditorChange(buffer: string): void;
    /** Route a pushed report: View re-syncs the editor + graphs; Edit raises the chip. */
    onReload(report: Report): void;
    /** Request a mode switch (from the toggle). */
    switchTo(mode: ServedMode): void;
}

/**
 * The View ⇄ Edit controller for a served session. View is read-only and auto-updating
 * (a disk change re-syncs the editor and graphs); Edit is editable and owns its buffers
 * (a disk change only raises the passive chip). Switching reconfigures both editors in
 * place — the buffers, cursors, and history survive — and prompts before discarding unsaved
 * edits when leaving Edit. At most one document is dirty at a time (navigation is locked
 * while dirty), so leaving Edit discards whichever one it is.
 */
export function createModeController(
    initial: ServedMode,
    ports: ModeControllerPorts,
): ModeController {
    let mode: ServedMode = initial;

    function apply(next: ServedMode): void {
        mode = next;
        ports.app.setEditable(next === "edit");
        ports.app.setConfigEditable(next === "edit");
        ports.setEditControlsVisible(next === "edit");
        ports.reflect(next);
    }

    apply(initial);

    return {
        get mode() {
            return mode;
        },
        onEditorChange(buffer) {
            if (mode === "edit") ports.dialogueLive.onEdit(buffer);
        },
        onConfigEditorChange(buffer) {
            if (mode === "edit") ports.configLive?.onEdit(buffer);
        },
        onReload(report) {
            if (mode === "edit") {
                ports.dialogueLive.onDiskChange(); // passive chip; never reload over edits
                return;
            }
            ports.app.showBanner(null);
            if (report.source != null) ports.app.setContent(report.source);
            ports.app.setDiagnostics(report.diagnostics ?? []);
            ports.app.setSemanticTokens(report.semanticTokens ?? []);
            ports.app.updateStages(report.stages);
        },
        switchTo(next) {
            if (next === mode) return;
            if (mode === "edit") {
                const dirty = ports.dialogueLive.dirty || (ports.configLive?.dirty ?? false);
                if (dirty && !ports.confirmDiscard()) return;
                ports.dialogueLive.discard();
                ports.configLive?.discard();
            }
            apply(next);
        },
    };
}
