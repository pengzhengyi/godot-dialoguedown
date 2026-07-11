/** The root-element class that maximizes the active tab and hides the app chrome. */
export const MAXIMIZED_CLASS = "maximized";

/** Selector for elements whose text entry must not be interrupted by the `f` shortcut. */
const TEXT_ENTRY = "input, textarea, select, [contenteditable], .cm-editor";

/** The page-level "maximize the active tab" mode. */
export interface Fullscreen {
    /** Enter full screen if minimized, leave it if maximized. */
    toggle(): void;
    /** Leave full screen (a no-op when already minimized). */
    exit(): void;
    /** Whether a tab is currently maximized. */
    isMaximized(): boolean;
}

/**
 * The whole-viewport "maximize" mode. A class on the root element hides the app chrome
 * (header + tabs, the status footer, the live banner) so the active graph or source split
 * fills the window. It is a plain CSS flag — deliberately *not* the browser Fullscreen
 * API — so it works in the offline single-file report and inside an embedding iframe,
 * where that API is commonly blocked. Toggle it with a maximize button or `f`; leave it
 * with `f` or Escape.
 */
export function initFullscreen(
    root: HTMLElement = document.body,
    doc: Document = document,
): Fullscreen {
    const isMaximized = (): boolean => root.classList.contains(MAXIMIZED_CLASS);

    const set = (on: boolean): void => {
        root.classList.toggle(MAXIMIZED_CLASS, on);
        // The icon glyph follows the root class in CSS; here we only reflect the state on
        // each button's label and tooltip, which CSS cannot express.
        doc.querySelectorAll<HTMLElement>(".maximize-button").forEach((button) => {
            button.setAttribute("aria-pressed", String(on));
            button.title = on ? "Exit full screen (Esc)" : "Full screen (f)";
            button.setAttribute("aria-label", on ? "Exit full screen" : "Full screen");
        });
    };

    const toggle = (): void => set(!isMaximized());
    const exit = (): void => set(false);

    doc.addEventListener("keydown", (event) => {
        // Yield to a handler that already acted (e.g. Escape closing the editor's search).
        if (event.defaultPrevented) return;
        if (event.key === "Escape") {
            if (isMaximized()) {
                exit();
                event.preventDefault();
            }
            return;
        }
        if (event.key.toLowerCase() === "f" && !event.ctrlKey && !event.metaKey && !event.altKey) {
            // Leave `f` alone while the reader is typing (the editor or a form field).
            const target = event.target as Element | null;
            if (target?.closest?.(TEXT_ENTRY)) return;
            toggle();
            event.preventDefault();
        }
    });

    return { toggle, exit, isMaximized };
}
