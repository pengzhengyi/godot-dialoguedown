/**
 * Lucide Icons (ISC): `panel-right-close` (a right-hand panel with an inward chevron)
 * to hide the panel, and `panel-right-open` (outward chevron) to bring it back — the
 * standard "hide/show side panel" glyphs. Both are rendered into the one button; CSS
 * reveals whichever matches the panel's collapsed state (a class on the panel's
 * container), so a toggle built while the panel is already collapsed still shows the
 * correct glyph.
 */
const icon = (variant: string, extra: string): string =>
    `<svg class="collapse-icon ${variant}" viewBox="0 0 24 24" width="15" height="15" ` +
    `fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" ` +
    `stroke-linejoin="round" aria-hidden="true">` +
    `<rect width="18" height="18" x="3" y="3" rx="2"/><path d="M15 3v18"/>${extra}</svg>`;

const COLLAPSE = icon("icon-collapse", '<path d="m8 9 3 3-3 3"/>');
const EXPAND = icon("icon-expand", '<path d="m10 15-3-3 3-3"/>');

/**
 * A hide/show toggle carrying both the collapse and expand panel glyphs. The visible
 * glyph is chosen by CSS from the container's collapsed class rather than per-button
 * state, so a toggle rebuilt while the panel is collapsed still reads correctly. The
 * mousedown is swallowed so pressing the toggle on a resize divider never starts a drag.
 */
export function createCollapseToggle(onToggle: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "collapse-toggle";
    button.innerHTML = COLLAPSE + EXPAND;
    button.addEventListener("click", onToggle);
    button.addEventListener("mousedown", (event) => event.stopPropagation());
    return button;
}

/** A right-side panel that can be hidden to give the main content the full width. */
export interface CollapsiblePanel {
    /** The toggle button to place on the panel's divider. */
    readonly button: HTMLButtonElement;
    /** Hide the panel if shown, show it if hidden. */
    toggle(): void;
    /** Whether the panel is currently hidden. */
    isCollapsed(): boolean;
}

export interface CollapsiblePanelOptions {
    /** The element that carries {@link collapsedClass} while the panel is hidden. */
    container: HTMLElement;
    /** The class toggled on {@link container} to hide the panel. */
    collapsedClass: string;
    /** localStorage key remembering the collapsed state across reloads. */
    storageKey: string;
    /** Accessible name of the panel, e.g. "inspector" or "preview". */
    name: string;
    /** Storage for the remembered state; defaults to `localStorage`. */
    storage?: Storage;
}

/**
 * Wire a right-side panel so a reader can hide it and bring it back. `container` carries
 * `collapsedClass` while hidden (CSS then hides the panel and lets the main pane fill),
 * and the choice is remembered in `localStorage` under `storageKey` — guarded, so a
 * `file://` report still works for the session. Returns a controller whose `button`
 * belongs on the panel's divider, where it doubles as the always-present re-open handle.
 */
export function initCollapsiblePanel(options: CollapsiblePanelOptions): CollapsiblePanel {
    const { container, collapsedClass, storageKey, name } = options;
    const storage = options.storage ?? defaultStorage();

    const isCollapsed = (): boolean => container.classList.contains(collapsedClass);

    const reflect = (collapsed: boolean): void => {
        container.classList.toggle(collapsedClass, collapsed);
        const label = collapsed ? `Show ${name}` : `Hide ${name}`;
        button.title = label;
        button.setAttribute("aria-label", label);
        button.setAttribute("aria-expanded", String(!collapsed));
    };

    const toggle = (): void => {
        const collapsed = !isCollapsed();
        reflect(collapsed);
        try {
            if (collapsed) storage?.setItem(storageKey, "1");
            else storage?.removeItem(storageKey);
        } catch {
            // storage unavailable (private mode / file://) — the applied state still holds
        }
    };

    const button = createCollapseToggle(toggle);

    let remembered = false;
    try {
        remembered = storage?.getItem(storageKey) === "1";
    } catch {
        // storage unavailable (private mode / file://) — default to expanded
    }
    reflect(remembered);

    return { button, toggle, isCollapsed };
}

/** `localStorage`, or `undefined` when it is not available (e.g. a sandboxed `file://`). */
function defaultStorage(): Storage | undefined {
    try {
        return globalThis.localStorage;
    } catch {
        return undefined;
    }
}
