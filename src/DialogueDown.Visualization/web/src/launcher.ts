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

/** The side-effecting collaborators, injected so the UI is testable without a server. */
export interface LauncherPorts {
    browse(path: string): Promise<BrowseListing | null>;
    open(source: string, mode: string): Promise<string | null>;
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
    const actions = element("div", "launcher-actions");
    actions.append(modes.element, open);
    card.append(listing, actions);
    container.append(card);

    let selected: string | null = initial.source;

    const select = (source: string, item: HTMLElement): void => {
        selected = source;
        open.disabled = false;
        for (const chosen of listing.querySelectorAll("li.selected"))
            chosen.classList.remove("selected");
        item.classList.add("selected");
    };

    const render = (list: BrowseListing): void => {
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
    };

    const browse = async (path: string): Promise<void> => {
        const list = await ports.browse(path);
        if (list) render(list);
    };

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
