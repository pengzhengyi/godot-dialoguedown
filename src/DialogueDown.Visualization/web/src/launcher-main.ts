import "@picocss/pico/css/pico.min.css";
import "./styles.css";

import { initLauncher, type BrowseListing, type LaunchSelection } from "./launcher";

/**
 * The .NET library replaces the `"__LAUNCHER__"` slot in launcher.html with the initial
 * selection JSON, so `window.__DD_LAUNCHER__` is an object at runtime. In local
 * development the placeholder is left as-is and a default is used instead.
 */
function resolveSelection(): LaunchSelection {
    const raw = (window as unknown as { __DD_LAUNCHER__?: unknown }).__DD_LAUNCHER__;
    if (raw && typeof raw === "object") return raw as LaunchSelection;
    return { root: ".", source: null, mode: "view" };
}

const ports = {
    async browse(path: string): Promise<BrowseListing | null> {
        const response = await fetch(`/api/browse?path=${encodeURIComponent(path)}`);
        return response.ok ? ((await response.json()) as BrowseListing) : null;
    },
    async open(source: string, mode: string): Promise<string | null> {
        const response = await fetch("/api/open", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ source, mode }),
        });
        // The server answers 303 to the report; fetch follows it, so response.url is the
        // report URL. A rejected open (bad source) is not a redirect.
        return response.redirected ? response.url : null;
    },
    navigate(url: string): void {
        window.location.assign(url);
    },
};

const container = document.getElementById("launcher");
if (container) initLauncher(container, resolveSelection(), ports);
