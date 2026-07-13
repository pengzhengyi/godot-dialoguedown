import type { AppController } from "./app";
import type { LiveEditPorts } from "./live-edit";
import type { Report } from "./model";

const SAVE_URL = "/api/save";

/** The Live Edit ports plus a switch to show or hide the edit controls (Edit only). */
export interface LiveEditUi extends LiveEditPorts {
    /** Show or hide the Save and Discard buttons as the session enters or leaves Edit. */
    setEditControlsVisible(visible: boolean): void;
}

/**
 * The browser-side ports for the Live Edit state machine: it saves the buffer over
 * `POST /api/save`, applies the recompiled graphs, restores the editor on discard, toggles
 * the Source tab's dirty marker and the "file changed on disk" chip, and arms a `beforeunload`
 * guard so a refresh/close with unsaved edits prompts first. The Save/Discard buttons and the
 * Save shortcut only act in Edit; the buttons are shown/hidden via
 * {@link LiveEditUi.setEditControlsVisible}.
 */
export function initLiveEditUi(
    app: AppController,
    requestSave: () => void,
    requestDiscard: () => void,
): LiveEditUi {
    const tabsEl = document.getElementById("tabs")!;
    const chip = createDiskChip();
    // Discard sits left of Save (secondary, destructive) — append it first.
    const discardButton = createDiscardButton(requestDiscard);
    const saveButton = createSaveButton(requestSave);
    installSaveShortcut(requestSave);
    let unloadHandler: ((event: BeforeUnloadEvent) => void) | null = null;

    return {
        async save(source) {
            const response = await fetch(SAVE_URL, {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ source }),
            });
            if (!response.ok) {
                app.showBanner("Save failed — the file could not be written.");
                return null;
            }
            app.showBanner(null);
            return ((await response.json()) as Report).stages;
        },
        updateStages: (stages) => app.updateStages(stages),
        setContent: (source) => app.setContent(source),
        setDirty: (dirty) => {
            tabsEl.querySelector(".tab")?.classList.toggle("dirty", dirty);
            saveButton.setEnabled(dirty);
            discardButton.setEnabled(dirty);
        },
        setDiskChanged: (changed) => {
            chip.hidden = !changed;
        },
        setUnloadGuard: (active) => {
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
        },
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
