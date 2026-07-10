/**
 * The launcher client: browse the local filesystem (anywhere, like Open Folder) for a
 * `.dialogue.md` source, choose the **root** it is served from, pick a **mode**, and open
 * its report. Browsing is unconfined; the server confines serving to the chosen root. The
 * DOM building lives here (unit-tested with jsdom); the browser wiring — CSS imports,
 * `fetch`, and navigation — lives in `launcher-main.ts`.
 */

/** The initial selection injected into the page by the server (absolute paths). */
export interface LaunchSelection {
    root: string;
    source: string | null;
    mode: string;
}

/** A directory listing from `GET /api/browse` — absolute paths. */
export interface BrowseListing {
    path: string;
    parent: string | null;
    directories: string[];
    sources: string[];
}

/** The side-effecting collaborators, injected so the UI is testable without a server. */
export interface LauncherPorts {
    browse(path: string): Promise<BrowseListing | null>;
    open(root: string, source: string, mode: string): Promise<string | null>;
    navigate(url: string): void;
}

interface ModeOption {
    value: string;
    label: string;
    help: string;
    disabled?: boolean;
}

const MODES: readonly ModeOption[] = [
    { value: "static", label: "Static", help: "Render the report once and open it." },
    {
        value: "watch",
        label: "Watch",
        help: "Serve the report and refresh it whenever the file changes on disk.",
    },
    {
        value: "live",
        label: "Live Edit",
        help: "Edit the script in the browser and save back — coming in a later version.",
        disabled: true,
    },
];

/** The last segment of a filesystem path (handles `/` and `\`). */
export function leafName(path: string): string {
    const parts = path.split(/[\\/]/).filter(Boolean);
    return parts.length > 0 ? parts[parts.length - 1] : path;
}

/** The parent directory of a path, or the path itself at a filesystem root. */
export function dirName(path: string): string {
    const match = path.replace(/[\\/]+$/, "").match(/^(.*)[\\/][^\\/]+$/);
    return match ? match[1] || "/" : path;
}

/** Whether `path` is `root` or sits inside it. */
export function isUnder(root: string, path: string): boolean {
    if (path === root) return true;
    const separator = root.includes("\\") ? "\\" : "/";
    return path.startsWith(root.endsWith(separator) ? root : root + separator);
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
            "Browse for a .dialogue.md script, choose the root it is served from, pick a mode, and open its report.",
        ),
    );

    const current = withText("p", "launcher-current", "");
    current.title = "Current folder";
    const rootRow = element("p", "launcher-root");
    const rootPath = withText("span", "launcher-root-path", initial.root);
    const useCurrent = withText("button", "launcher-set-root", "Use current folder");
    (useCurrent as HTMLButtonElement).type = "button";
    rootRow.append(
        document.createTextNode("Serving root: "),
        rootPath,
        document.createTextNode(" "),
        useCurrent,
    );

    const listing = element("ul", "launcher-listing");
    const modes = renderModes(initial.mode);
    const open = withText("button", "launcher-open", "Open") as HTMLButtonElement;
    open.type = "button";
    open.disabled = initial.source === null;
    card.append(current, rootRow, listing, modes.element, open);
    container.append(card);

    let root = initial.root;
    let selected: string | null = initial.source;
    let here = initial.source ? dirName(initial.source) : initial.root;

    const setRoot = (value: string): void => {
        root = value;
        rootPath.textContent = root;
        rootPath.title = root;
    };

    const select = (source: string, item: HTMLElement): void => {
        selected = source;
        if (!isUnder(root, source)) setRoot(dirName(source));
        open.disabled = false;
        for (const chosen of listing.querySelectorAll("li.selected"))
            chosen.classList.remove("selected");
        item.classList.add("selected");
    };

    const render = (list: BrowseListing): void => {
        here = list.path;
        current.textContent = list.path;
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

    useCurrent.addEventListener("click", () => {
        setRoot(here);
        if (selected !== null && !isUnder(root, selected)) {
            selected = null;
            open.disabled = true;
            for (const chosen of listing.querySelectorAll("li.selected"))
                chosen.classList.remove("selected");
        }
    });

    open.addEventListener("click", () => {
        if (selected === null) return;
        void ports.open(root, selected, modes.value()).then((url) => {
            if (url) ports.navigate(url);
        });
    });

    void browse(here);
}

function row(label: string, kind: string, onClick: () => void): HTMLElement {
    const item = withText("li", `launcher-item ${kind}`, label);
    item.addEventListener("click", onClick);
    return item;
}

function renderModes(initialMode: string): { element: HTMLElement; value: () => string } {
    const group = element("div", "launcher-modes");
    group.setAttribute("role", "radiogroup");
    group.setAttribute("aria-label", "Mode");
    for (const mode of MODES) {
        const label = element("label", `launcher-mode${mode.disabled ? " disabled" : ""}`);
        label.title = mode.help;
        const input = document.createElement("input");
        input.type = "radio";
        input.name = "launcher-mode";
        input.value = mode.value;
        input.disabled = mode.disabled ?? false;
        input.checked = mode.value === initialMode && !mode.disabled;
        label.append(input, document.createTextNode(` ${mode.label}`));
        group.append(label);
    }

    if (!group.querySelector("input:checked")) {
        const fallback = group.querySelector<HTMLInputElement>("input:not([disabled])");
        if (fallback) fallback.checked = true;
    }

    return {
        element: group,
        value: () => group.querySelector<HTMLInputElement>("input:checked")?.value ?? "static",
    };
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
