const CREATE_CONFIG_URL = "/api/create-config";
const OPEN_CONFIG_KEY = "dd-open-config-after-create";

/** The side effects the create-config flow drives, injected so the logic is testable. */
export interface ConfigCreatePorts {
    /** POST the create request to the server; resolves to its response. */
    post(): Promise<Response>;
    /** Reload the page — the reloaded report is compiled with the new configuration. */
    reload(): void;
}

/**
 * Create a `dialogue.toml` for a project that has none. On success it flags the reloaded page
 * to open on the Config tab and reloads (the reloaded report carries the new configuration, so
 * the Config tab becomes the Stage 2 editor). On failure it throws with a reader-facing message
 * — a `409` means one already exists, so the reader is told to reload and edit it.
 */
export async function createConfig(ports: ConfigCreatePorts): Promise<void> {
    const response = await ports.post();
    if (response.ok) {
        rememberOpenConfigTab();
        ports.reload();
        return;
    }
    throw new Error(await errorMessage(response));
}

/** The production ports: POST the route and reload through `window.location`. */
export function browserConfigCreatePorts(): ConfigCreatePorts {
    return {
        post: () => fetch(CREATE_CONFIG_URL, { method: "POST" }),
        reload: () => window.location.reload(),
    };
}

/**
 * Whether a just-created config asked the reloaded page to open on the Config tab. Reads the
 * one-shot flag and clears it, so only the reload right after a create lands on Config.
 */
export function consumeOpenConfigTab(): boolean {
    try {
        if (window.sessionStorage.getItem(OPEN_CONFIG_KEY) === "1") {
            window.sessionStorage.removeItem(OPEN_CONFIG_KEY);
            return true;
        }
    } catch {
        // A blocked sessionStorage just means we open on the default tab — not fatal.
    }
    return false;
}

async function errorMessage(response: Response): Promise<string> {
    const fallback = "The configuration file could not be created.";
    try {
        const body = (await response.json()) as { message?: string };
        return body.message ?? fallback;
    } catch {
        return fallback;
    }
}

function rememberOpenConfigTab(): void {
    try {
        window.sessionStorage.setItem(OPEN_CONFIG_KEY, "1");
    } catch {
        // A blocked sessionStorage just means we open on the default tab — not fatal.
    }
}
