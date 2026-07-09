import { describe, it, expect, beforeEach } from "vitest";
import { initModeBadge, MODE_INFO } from "./mode-badge";

describe("initModeBadge", () => {
    beforeEach(() => {
        document.body.innerHTML = `<span id="mode-badge" class="mode-badge"></span>`;
    });

    it("shows the human label for the mode", () => {
        initModeBadge("watch");
        expect(document.getElementById("mode-badge")!.textContent).toBe("Hot Reload");
    });

    it("adds a mode-specific class and an accessible description", () => {
        const badge = initModeBadge("static");
        expect(badge.classList.contains("mode-static")).toBe(true);
        expect(badge.getAttribute("aria-label")).toContain(MODE_INFO.static.description);
    });

    it("labels live mode", () => {
        initModeBadge("live");
        expect(document.getElementById("mode-badge")!.textContent).toBe("Live");
    });
});

describe("MODE_INFO", () => {
    it("has a distinct label and description for every mode", () => {
        const modes = ["static", "watch", "live"] as const;
        const labels = modes.map((m) => MODE_INFO[m].label);
        expect(new Set(labels).size).toBe(modes.length);
        for (const mode of modes) {
            expect(MODE_INFO[mode].description.length).toBeGreaterThan(10);
        }
    });
});
