import tippy from "tippy.js";

/** Split a path into its directory (head) and last segment (tail, with separator). */
export function splitPath(path: string): { head: string; tail: string } {
    const index = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
    if (index < 0) return { head: "", tail: path };
    return { head: path.slice(0, index), tail: path.slice(index) };
}

/** Copy text to the clipboard, falling back to a hidden textarea where the Clipboard API is unavailable. */
export async function copyToClipboard(text: string): Promise<void> {
    if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
        return;
    }
    const area = document.createElement("textarea");
    area.value = text;
    area.style.position = "fixed";
    area.style.opacity = "0";
    document.body.appendChild(area);
    area.select();
    document.execCommand("copy");
    area.remove();
}

/**
 * Show the document path in the status bar: the filename always shows while the
 * middle of the directory is ellipsised (CSS), the full path is a hover tooltip,
 * and clicking copies the path. Hidden when there is no path (e.g. a library
 * render with no file).
 */
export function initPathDisplay(path: string | undefined): HTMLElement | null {
    const button = document.getElementById("doc-path") as HTMLButtonElement | null;
    if (!button) return null;
    if (!path) {
        button.hidden = true;
        return button;
    }

    const { head, tail } = splitPath(path);
    button.querySelector(".path-head")!.textContent = head;
    button.querySelector(".path-tail")!.textContent = tail;
    button.hidden = false;

    const tip = tippy(button, { content: `${path}\n(click to copy)`, maxWidth: 480 });
    button.addEventListener("click", () => {
        void copyToClipboard(path).then(() => {
            tip.setContent("Copied!");
            window.setTimeout(() => tip.setContent(`${path}\n(click to copy)`), 1200);
        });
    });
    return button;
}
