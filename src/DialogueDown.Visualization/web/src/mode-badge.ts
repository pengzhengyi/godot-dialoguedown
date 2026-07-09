import tippy from "tippy.js";
import type { VisualizationMode } from "./model";

interface ModeInfo {
    label: string;
    description: string;
}

/** The badge label and hover description for each mode. */
export const MODE_INFO: Record<VisualizationMode, ModeInfo> = {
    static: {
        label: "Static",
        description:
            "A one-shot, self-contained report. It will not change if the source file is edited.",
    },
    watch: {
        label: "Hot Reload",
        description:
            "Served by a local server watching the file. Save the file in your editor and the report updates automatically.",
    },
    live: {
        label: "Live",
        description:
            "Served by a local server with in-browser editing. Edit here and save to update the file and the report.",
    },
};

/**
 * Fill the mode badge for the current mode and attach a hover tooltip describing
 * what that mode does.
 */
export function initModeBadge(mode: VisualizationMode): HTMLElement {
    const badge = document.getElementById("mode-badge")!;
    const info = MODE_INFO[mode];
    badge.textContent = info.label;
    badge.classList.add(`mode-${mode}`);
    badge.setAttribute("aria-label", `Mode: ${info.label}. ${info.description}`);
    tippy(badge, { content: info.description, maxWidth: 280 });
    return badge;
}
