import type { AppController } from "./app";
import type { LiveEditPorts } from "./live-edit";
import type { Report } from "./model";

const SAVE_URL = "/api/save";

/** Binds one editable document to the shared Live Edit chrome (Save/Discard, dirty, unload guard). */
export interface DocumentBinding {
    /** The save target: omitted for the dialogue document, `"config"` for `dialogue.toml`. */
    target?: "config";
    /** Toggle this document's own dirty cue (its tab marker, and any per-document staleness). */
    markDirty(dirty: boolean): void;
    /** Replace this document's editor content — used to restore the last saved version on discard. */
    setContent(source: string): void;
    /** Apply anything in the recompiled report beyond the graphs (e.g. refresh the config speakers). */
    onSaved?(report: Report): void;
}

/** The shared Live Edit chrome: per-document ports plus a switch to show or hide the controls. */
export interface LiveEditUi {
    /** Build the Live Edit ports for one editable document, sharing the Save/dirty chrome. */
    portsFor(binding: DocumentBinding): LiveEditPorts;
    /** Show or hide the Save and Discard buttons as the session enters or leaves Edit. */
    setEditControlsVisible(visible: boolean): void;
}

/**
 * The browser-side chrome for the Live Edit state machine: a shared Save/Discard pair, the
 * Save shortcut, the "file changed on disk" chip, and a `beforeunload` guard — plus a
 * {@link LiveEditUi.portsFor} factory that binds them to one editable document (the dialogue
 * or its `dialogue.toml`). Saving posts the buffer to `POST /api/save` with the document's
 * target, applies the recompiled graphs, and restores the editor on discard. The Save/Discard
 * buttons and the shortcut route to the active document via `requestSave` / `requestDiscard`.
 */
export function initLiveEditUi(
    app: AppController,
    requestSave: () => void,
    requestDiscard: () => void,
): LiveEditUi {
    const chip = createDiskChip();
    // Discard sits left of Save (secondary, destructive) — append it first.
    const discardButton = createDiscardButton(requestDiscard);
    const saveButton = createSaveButton(requestSave);
    installSaveShortcut(requestSave);
    let unloadHandler: ((event: BeforeUnloadEvent) => void) | null = null;

    function setUnloadGuard(active: boolean): void {
        if (active && unloadHandler === null) {
            unloadHandler = (event) => {
                event.preventDefault();
                event.returnValue = "";
            };
            window.addEventListener("beforeunload", unloadHandler);
        } else if (!active && unloadHandler !== null) {
            window.removeEventListener("beforeunload", unloadHandler);
            unloadHandler = null;
        }
    }

    return {
        portsFor: (binding) => ({
            async save(source) {
                const response = await fetch(SAVE_URL, {
                    method: "POST",
                    headers: { "content-type": "application/json" },
                    body: JSON.stringify({ source, target: binding.target }),
                });
                if (!response.ok) {
                    app.showBanner("Save failed — the file could not be written.");
                    return null;
                }
                app.showBanner(null);
                const report = (await response.json()) as Report;
                binding.onSaved?.(report);
                return report.stages;
            },
            updateStages: (stages) => app.updateStages(stages),
            setContent: (source) => binding.setContent(source),
            setDirty: (dirty) => {
                binding.markDirty(dirty);
                saveButton.setEnabled(dirty);
                discardButton.setEnabled(dirty);
            },
            setDiskChanged: (changed) => {
                chip.hidden = !changed;
            },
            setUnloadGuard,
        }),
        setEditControlsVisible: (visible) => {
            discardButton.setVisible(visible);
            saveButton.setVisible(visible);
        },
    };
}

/** A passive, clickable chip in the header: the file diverged on disk — click to refresh. */
function createDiskChip(): HTMLElement {
    const chip = document.createElement("button");
    chip.className = "disk-chip";
    chip.type = "button";
    chip.hidden = true;
    chip.textContent = "File changed on disk — refresh to sync";
    chip.title = "Reload to load the file's current contents. Unsaved edits will be lost.";
    chip.addEventListener("click", () => window.location.reload());
    document.querySelector(".app-header")?.appendChild(chip);
    return chip;
}

/**
 * A Save button in the status bar (the explicit affordance beside the keyboard
 * shortcut). It is shown only in Edit and enabled only while there are unsaved edits.
 */
function createSaveButton(onSave: () => void): {
    setEnabled(enabled: boolean): void;
    setVisible(visible: boolean): void;
} {
    const button = document.createElement("button");
    button.className = "save-button";
    button.type = "button";
    button.textContent = "Save";
    button.title = "Save changes to the file (Ctrl+S / ⌘S)";
    button.disabled = true;
    button.hidden = true;
    button.addEventListener("click", onSave);
    document.querySelector(".status-bar")?.appendChild(button);
    return {
        setEnabled: (enabled) => {
            button.disabled = !enabled;
        },
        setVisible: (visible) => {
            button.hidden = !visible;
        },
    };
}

/**
 * A Discard button beside Save. It restores the editor to the last saved version and is
 * shown only in Edit and enabled only while there are unsaved edits. Because discarding is
 * destructive, it confirms first — matching the mode-switch discard prompt.
 */
function createDiscardButton(onDiscard: () => void): {
    setEnabled(enabled: boolean): void;
    setVisible(visible: boolean): void;
} {
    const button = document.createElement("button");
    button.className = "discard-button";
    button.type = "button";
    button.textContent = "Discard";
    button.title = "Discard unsaved changes and restore the last saved version";
    button.disabled = true;
    button.hidden = true;
    button.addEventListener("click", () => {
        if (window.confirm("Discard all unsaved changes and restore the last saved version?")) {
            onDiscard();
        }
    });
    document.querySelector(".status-bar")?.appendChild(button);
    return {
        setEnabled: (enabled) => {
            button.disabled = !enabled;
        },
        setVisible: (visible) => {
            button.hidden = !visible;
        },
    };
}

/**
 * Install the Save shortcut on the whole document so it works from any tab and
 * regardless of focus (not only when the editor is focused). Intercepting both
 * Ctrl+S and ⌘S prevents the browser's own "save page" dialog either way. (In View the
 * buffer is never dirty, so a save is a no-op.)
 */
function installSaveShortcut(onSave: () => void): void {
    document.addEventListener("keydown", (event) => {
        if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === "s") {
            event.preventDefault();
            onSave();
        }
    });
}
