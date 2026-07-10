import type { AppController } from "./app";
import type { LiveEditPorts } from "./live-edit";
import type { Report } from "./model";

const SAVE_URL = "/api/save";
const EVENTS_URL = "/api/events";

/**
 * The browser-side ports for the Live Edit state machine: it saves the buffer over
 * `POST /api/save`, applies the recompiled graphs, toggles the Source tab's dirty
 * marker and the "file changed on disk" chip, and arms a `beforeunload` guard so a
 * refresh/close with unsaved edits prompts first.
 */
export function initLiveEditUi(app: AppController, requestSave: () => void): LiveEditPorts {
    const tabsEl = document.getElementById("tabs")!;
    const chip = createDiskChip();
    const setSaveEnabled = createSaveButton(requestSave);
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
        setDirty: (dirty) => {
            tabsEl.querySelector(".tab")?.classList.toggle("dirty", dirty);
            setSaveEnabled(dirty);
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
    };
}

/**
 * Subscribe to the disk stream in Live Edit: an external change raises the passive
 * chip (never a reload, so the editor is never clobbered).
 */
export function watchDiskChanges(onDiskChange: () => void): EventSource {
    const events = new EventSource(EVENTS_URL);
    events.addEventListener("reload", () => onDiskChange());
    return events;
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
 * A Save button in the status bar (the explicit affordance beside ⌘/Ctrl+S). It is
 * enabled only while there are unsaved edits. Returns a setter to reflect that state.
 */
function createSaveButton(onSave: () => void): (enabled: boolean) => void {
    const button = document.createElement("button");
    button.className = "save-button";
    button.type = "button";
    button.textContent = "Save";
    button.title = "Save changes to the file (⌘/Ctrl+S)";
    button.disabled = true;
    button.addEventListener("click", onSave);
    document.querySelector(".status-bar")?.appendChild(button);
    return (enabled) => {
        button.disabled = !enabled;
    };
}
