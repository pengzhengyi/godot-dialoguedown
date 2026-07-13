import type { AppController } from "./app";
import type { LiveEditController } from "./live-edit";
import type { Report, ServedMode } from "./model";

/** The side-effecting collaborators the mode controller drives (injected for testing). */
export interface ModeControllerPorts {
    /** Reconfigures the editor (editable/read-only, content) and the graph tabs. */
    app: AppController;
    /** The Edit-mode dirty/save state machine. */
    live: LiveEditController;
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
    /** Route an editor change: dirty tracking in Edit, ignored in View (a reload). */
    onEditorChange(buffer: string): void;
    /** Route a pushed report: View re-syncs the editor + graphs; Edit raises the chip. */
    onReload(report: Report): void;
    /** Request a mode switch (from the toggle). */
    switchTo(mode: ServedMode): void;
}

/**
 * The View ⇄ Edit controller for a served session. View is read-only and auto-updating
 * (a disk change re-syncs the editor and graphs); Edit is editable and owns its buffer
 * (a disk change only raises the passive chip). Switching reconfigures the one editor in
 * place — the buffer, cursor, and history survive — and prompts before discarding unsaved
 * edits when leaving Edit.
 */
export function createModeController(
    initial: ServedMode,
    ports: ModeControllerPorts,
): ModeController {
    let mode: ServedMode = initial;

    function apply(next: ServedMode): void {
        mode = next;
        ports.app.setEditable(next === "edit");
        ports.setEditControlsVisible(next === "edit");
        ports.reflect(next);
    }

    apply(initial);

    return {
        get mode() {
            return mode;
        },
        onEditorChange(buffer) {
            if (mode === "edit") ports.live.onEdit(buffer);
        },
        onReload(report) {
            if (mode === "edit") {
                ports.live.onDiskChange(); // passive chip; never reload over edits
                return;
            }
            ports.app.showBanner(null);
            if (report.source != null) ports.app.setContent(report.source);
            ports.app.updateStages(report.stages);
        },
        switchTo(next) {
            if (next === mode) return;
            if (mode === "edit" && ports.live.dirty && !ports.confirmDiscard()) return;
            if (mode === "edit") ports.live.discard();
            apply(next);
        },
    };
}
