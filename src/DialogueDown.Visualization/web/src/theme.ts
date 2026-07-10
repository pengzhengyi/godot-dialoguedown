/**
 * The reader's color-theme choice. "system" follows the OS (no override); "light" and
 * "dark" force the theme. The choice drives Pico's `data-theme` attribute (and the
 * editor's `--md-*` colors via CSS), and is remembered in `localStorage`.
 */
export type ThemePreference = "system" | "light" | "dark";

const STORAGE_KEY = "dd-theme";
const OPTIONS: ReadonlyArray<[ThemePreference, string]> = [
    ["system", "System"],
    ["light", "Light"],
    ["dark", "Dark"],
];

/** Read the stored preference, defaulting to "system" (and tolerating disabled storage). */
export function readThemePreference(storage: Storage = localStorage): ThemePreference {
    try {
        const value = storage.getItem(STORAGE_KEY);
        return value === "light" || value === "dark" ? value : "system";
    } catch {
        return "system";
    }
}

/**
 * Apply a preference: set (or clear, for "system") `data-theme` on the root element so
 * Pico and the editor colors follow, and persist the choice. Storage failures (e.g. a
 * `file://` report) are ignored — the theme still applies for the session.
 */
export function applyThemePreference(
    preference: ThemePreference,
    root: HTMLElement = document.documentElement,
    storage: Storage = localStorage,
): void {
    if (preference === "system") {
        root.removeAttribute("data-theme");
    } else {
        root.setAttribute("data-theme", preference);
    }
    try {
        if (preference === "system") {
            storage.removeItem(STORAGE_KEY);
        } else {
            storage.setItem(STORAGE_KEY, preference);
        }
    } catch {
        // storage unavailable (private mode / file://) — the applied theme still holds
    }
}

/**
 * Build the theme `<select>` (System / Light / Dark), reflecting the initial choice and
 * applying each change through {@link apply}.
 */
export function createThemeSelect(
    apply: (preference: ThemePreference) => void = applyThemePreference,
    initial: ThemePreference = readThemePreference(),
): HTMLSelectElement {
    const select = document.createElement("select");
    select.className = "theme-select";
    select.setAttribute("aria-label", "Color theme");
    select.title = "Color theme";
    for (const [value, label] of OPTIONS) {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = label;
        select.append(option);
    }
    select.value = initial;
    select.addEventListener("change", () => apply(select.value as ThemePreference));
    return select;
}

/** Apply the stored preference on load and mount the toggle into the header controls. */
export function initTheme(host: Element | null): void {
    applyThemePreference(readThemePreference());
    host?.prepend(createThemeSelect());
}
