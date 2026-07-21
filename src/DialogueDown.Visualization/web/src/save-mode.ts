/** When a dirty buffer is scheduled to save: `auto` after idle, or `manual` only on request. */
export type SaveMode = "auto" | "manual";

/** The two editable document types, each with its own persisted save-mode preference. */
export type DocumentType = "source" | "config";

/** The `localStorage` key each document type persists its {@link SaveMode} under. */
const STORAGE_KEY: Record<DocumentType, string> = {
    source: "dd-save-mode-source",
    config: "dd-save-mode-config",
};

/**
 * Each document type's default {@link SaveMode}: Source starts Auto (the primary writer
 * surface), while Config starts Manual because transient TOML is often invalid mid-edit.
 */
const DEFAULT_MODE: Record<DocumentType, SaveMode> = {
    source: "auto",
    config: "manual",
};

/** Reads and persists one document type's {@link SaveMode} preference. */
export interface SaveModeStore {
    /** The persisted mode, or this document type's default when none is stored. */
    get(): SaveMode;
    /** Persist a new mode for this document type. */
    set(mode: SaveMode): void;
}

function isSaveMode(value: string | null): value is SaveMode {
    return value === "auto" || value === "manual";
}

/**
 * A {@link SaveModeStore} backed by `localStorage`, so a document type's Auto/Manual choice
 * survives across report sessions. Reads fall back to the document type's default when nothing
 * is stored or storage is unavailable; writes swallow storage errors (a blocked `localStorage`
 * just means the choice is not remembered, never a crash).
 */
export function createSaveModeStore(
    type: DocumentType,
    storage: Pick<Storage, "getItem" | "setItem"> | null = safeLocalStorage(),
): SaveModeStore {
    const key = STORAGE_KEY[type];
    const fallback = DEFAULT_MODE[type];
    return {
        get() {
            if (storage === null) return fallback;
            try {
                const stored = storage.getItem(key);
                return isSaveMode(stored) ? stored : fallback;
            } catch {
                return fallback;
            }
        },
        set(mode) {
            if (storage === null) return;
            try {
                storage.setItem(key, mode);
            } catch {
                // A blocked localStorage just means the choice is not remembered — not fatal.
            }
        },
    };
}

/** The default document mode, exposed for tests and callers that need it without a store. */
export function defaultSaveMode(type: DocumentType): SaveMode {
    return DEFAULT_MODE[type];
}

function safeLocalStorage(): Pick<Storage, "getItem" | "setItem"> | null {
    try {
        return window.localStorage;
    } catch {
        return null;
    }
}
