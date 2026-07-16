/**
 * The launcher client: browse the launch root for `.dialogue.md` sources, pick one and a
 * mode, and open its report. The DOM building lives here (unit-tested with jsdom); the
 * browser wiring — CSS imports, `fetch`, and navigation — lives in `launcher-main.ts`.
 */

import { createModeToggle } from "./mode-toggle";
import type { ServedMode } from "./model";

/** The initial selection injected into the page by the server. */
export interface LaunchSelection {
    root: string;
    source: string | null;
    mode: string;
}

/** A directory listing under the launch root, as returned by `GET /api/browse`. */
export interface BrowseListing {
    path: string;
    parent: string | null;
    directories: string[];
    sources: string[];
}

/** The required extension for a DialogueDown script (auto-appended when creating). */
export const SCRIPT_EXTENSION = ".dialogue.md";

/** The outcome of a create request. */
export type CreateOutcome =
    | { kind: "opened"; url: string }
    | { kind: "exists"; path: string }
    | { kind: "error"; message: string };

/** The side-effecting collaborators, injected so the UI is testable without a server. */
export interface LauncherPorts {
    browse(path: string): Promise<BrowseListing | null>;
    open(source: string, mode: string): Promise<string | null>;
    create(path: string): Promise<CreateOutcome>;
    confirm(message: string): boolean;
    navigate(url: string): void;
}

/** The last path segment of a root-relative path (a display label). */
export function leafName(path: string): string {
    const parts = path.split("/").filter(Boolean);
    return parts.length > 0 ? parts[parts.length - 1] : path;
}

/** The parent (root-relative) of a path — the empty string at the root. */
export function parentPath(path: string): string {
    const parts = path.split("/").filter(Boolean);
    parts.pop();
    return parts.join("/");
}

/** Builds the launcher UI into <paramref /> and wires it to <paramref />. */
export function initLauncher(
    container: HTMLElement,
    initial: LaunchSelection,
    ports: LauncherPorts,
): void {
    container.replaceChildren();
    const card = element("section", "launcher-card");
    card.append(
        withText("h1", "launcher-title", "Open a script"),
        withText(
            "p",
            "launcher-subtitle",
            "Choose a .dialogue.md script under the root, pick a mode, and open its compilation report.",
        ),
    );

    const rootRow = element("p", "launcher-root");
    rootRow.append(document.createTextNode("Root: "));
    const rootPath = withText("span", "launcher-root-path", initial.root);
    rootPath.title = initial.root;
    rootRow.append(rootPath);
    card.append(rootRow);

    const listing = element("ul", "launcher-listing");
    // The chosen mode drives the accent (blue View / green Edit) on the capsule, the
    // selected row, and the Open button — the same `--mode-accent` the report uses,
    // scoped to the launcher via data-served-mode on the container.
    const initialMode: ServedMode = initial.mode === "edit" ? "edit" : "view";
    container.dataset.servedMode = initialMode;
    const modes = renderModes(initialMode, (mode) => {
        container.dataset.servedMode = mode;
    });
    const open = withText("button", "launcher-open", "Open") as HTMLButtonElement;
    open.type = "button";
    open.disabled = true;

    let selected: string | null = initial.source;
    // The folder currently shown; a newly created file lands here.
    let currentPath = "";
    let creating = false;

    // The "New file" affordance is the last row of the listing, so a file is named and
    // created alongside the others in the tree. Collapsed it is a trigger; clicking it turns
    // the row into an inline name field. The error sits just below the listing.
    const newRow = document.createElement("li");
    const createError = withText("p", "launcher-create-error", "");
    createError.setAttribute("role", "alert");
    createError.hidden = true;

    const select = (source: string, item: HTMLElement): void => {
        selected = source;
        open.disabled = false;
        for (const chosen of listing.querySelectorAll("li.selected"))
            chosen.classList.remove("selected");
        item.classList.add("selected");
    };

    const showError = (message: string): void => {
        createError.textContent = message;
        createError.hidden = false;
    };

    const submitCreate = async (name: string): Promise<void> => {
        const typed = name.trim();
        if (typed === "") {
            showError("Enter a name.");
            return;
        }
        const fileName = typed.endsWith(SCRIPT_EXTENSION) ? typed : `${typed}${SCRIPT_EXTENSION}`;
        const outcome = await ports.create(currentPath ? `${currentPath}/${fileName}` : fileName);
        if (outcome.kind === "opened") {
            ports.navigate(outcome.url);
        } else if (outcome.kind === "exists") {
            if (ports.confirm(`A file named ${fileName} already exists here. Open it instead?`)) {
                const url = await ports.open(outcome.path, "edit");
                if (url) ports.navigate(url);
            }
        } else {
            showError(outcome.message);
        }
    };

    const stopCreating = (): void => {
        creating = false;
        createError.hidden = true;
        renderNewRow();
    };

    // Rebuilds the trailing listing row for the current create state: a "New file" trigger,
    // or the inline name field (input + `.dialogue.md` suffix + Create/Cancel) once creating.
    const renderNewRow = (): void => {
        if (!creating) {
            newRow.className = "launcher-item launcher-new";
            newRow.onclick = () => {
                creating = true;
                createError.hidden = true;
                renderNewRow();
            };
            newRow.replaceChildren(document.createTextNode("New file"));
            return;
        }

        newRow.className = "launcher-item launcher-create-row";
        newRow.onclick = null;
        const name = document.createElement("input");
        name.type = "text";
        name.className = "launcher-create-name";
        name.placeholder = "Enter the new script name";
        name.setAttribute("aria-label", "New script name");
        const ext = withText("span", "launcher-create-ext", SCRIPT_EXTENSION);
        const submit = withText("button", "launcher-create-submit", "Create") as HTMLButtonElement;
        submit.type = "button";
        submit.onclick = () => void submitCreate(name.value);

        // The secondary button first clears a typed name, then — once the field is empty —
        // dismisses it back to the "New file" trigger, so one button both wipes a mistake
        // and cancels (the two outcomes Escape reaches directly).
        const dismiss = withText("button", "launcher-create-cancel", "Cancel") as HTMLButtonElement;
        dismiss.type = "button";
        const syncDismiss = (): void => {
            dismiss.textContent = name.value === "" ? "Cancel" : "Clear";
        };
        dismiss.onclick = (event) => {
            // Cancelling restores the trigger's own click handler on this same <li>; stop the
            // click here so it does not bubble up and immediately re-open the field.
            event.stopPropagation();
            if (name.value === "") {
                stopCreating();
                return;
            }
            name.value = "";
            createError.hidden = true;
            syncDismiss();
            name.focus();
        };
        name.oninput = syncDismiss;
        name.onkeydown = (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                void submitCreate(name.value);
            } else if (event.key === "Escape") {
                event.preventDefault();
                stopCreating();
            }
        };
        syncDismiss();
        newRow.replaceChildren(name, ext, submit, dismiss);
        name.focus();
    };

    const render = (list: BrowseListing): void => {
        currentPath = list.path;
        creating = false; // browsing to another folder cancels an in-progress create
        createError.hidden = true;
        listing.replaceChildren();
        if (list.parent !== null) {
            listing.append(row("..", "up", () => void browse(list.parent!)));
        }

        for (const dir of list.directories) {
            listing.append(row(leafName(dir), "dir", () => void browse(dir)));
        }

        for (const source of list.sources) {
            const item = row(leafName(source), "src", () => select(source, item));
            if (source === selected) select(source, item);
            listing.append(item);
        }

        if (list.parent === null && list.directories.length === 0 && list.sources.length === 0) {
            listing.append(withText("li", "launcher-empty", "No scripts or folders here."));
        }

        renderNewRow();
        listing.append(newRow);
    };

    const browse = async (path: string): Promise<void> => {
        const list = await ports.browse(path);
        if (list) render(list);
    };

    const actions = element("div", "launcher-actions");
    actions.append(modes.element, open);
    card.append(listing, createError, actions);
    container.append(card);

    open.addEventListener("click", () => {
        if (selected === null) return;
        void ports.open(selected, modes.value()).then((url) => {
            if (url) ports.navigate(url);
        });
    });

    void browse(initial.source ? parentPath(initial.source) : "");
}

function row(label: string, kind: string, onClick: () => void): HTMLElement {
    const item = withText("li", `launcher-item ${kind}`, label);
    item.addEventListener("click", onClick);
    return item;
}

function renderModes(
    initialMode: ServedMode,
    onChange: (mode: ServedMode) => void,
): { element: HTMLElement; value: () => ServedMode } {
    let current = initialMode;
    const toggle = createModeToggle(current, (mode) => {
        current = mode;
        toggle.reflect(mode);
        onChange(mode);
    });
    return { element: toggle.element, value: () => current };
}

function element(tag: string, className: string): HTMLElement {
    const node = document.createElement(tag);
    node.className = className;
    return node;
}

function withText(tag: string, className: string, text: string): HTMLElement {
    const node = element(tag, className);
    node.textContent = text;
    return node;
}
