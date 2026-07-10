import type { Stage } from "./model";

/**
 * The side-effecting collaborators the live-edit logic drives, injected so the state
 * machine is testable without a real editor, server, or DOM.
 */
export interface LiveEditPorts {
    /** Saves the buffer to disk; resolves to the recompiled stages, or `null` on failure. */
    save(source: string): Promise<Stage[] | null>;
    /** Applies recompiled stages to the report's graph tabs (leaving the editor alone). */
    updateStages(stages: Stage[]): void;
    /** Shows or hides the Source tab's unsaved (dirty) marker. */
    setDirty(dirty: boolean): void;
    /** Shows or hides the passive "file changed on disk — refresh to sync" chip. */
    setDiskChanged(changed: boolean): void;
    /** Arms or disarms the guard that warns before leaving with unsaved edits. */
    setUnloadGuard(active: boolean): void;
}

/** Drives the Source tab's edit → save → dirty/disk-changed lifecycle in Live Edit. */
export interface LiveEditController {
    /** The buffer changed — remember it, mark the session dirty, and guard against losing edits. */
    onEdit(buffer: string): void;
    /** Save (Cmd/Ctrl+S or the Save button) — write the current buffer, refresh the graphs, clear dirty. */
    save(): Promise<void>;
    /** The file changed on disk (external edit) — show the passive chip, never reload. */
    onDiskChange(): void;
    /** Whether the buffer has unsaved edits. */
    readonly dirty: boolean;
}

/**
 * The Live Edit state machine. Editing records the buffer and marks the session dirty
 * (and arms the unload guard); Save writes the current buffer, applies the recompiled
 * graphs, and clears dirty; a disk change only raises the passive chip — the editor is
 * never reloaded over your edits.
 */
export function createLiveEdit(ports: LiveEditPorts, initialBuffer = ""): LiveEditController {
    let dirty = false;
    let buffer = initialBuffer;

    function setDirty(next: boolean): void {
        dirty = next;
        ports.setDirty(next);
        ports.setUnloadGuard(next);
    }

    return {
        get dirty() {
            return dirty;
        },
        onEdit(next) {
            buffer = next;
            if (!dirty) setDirty(true);
        },
        async save() {
            if (!dirty) return; // nothing changed since the last save
            const stages = await ports.save(buffer);
            if (stages === null) return; // a failed save keeps the buffer dirty
            ports.updateStages(stages);
            setDirty(false);
            ports.setDiskChanged(false); // saving overwrites disk, so any earlier chip is stale
        },
        onDiskChange() {
            ports.setDiskChanged(true);
        },
    };
}
