import { describe, it, expect, beforeEach } from "vitest";
import { initModeBadge, MODE_INFO } from "./mode-badge";

describe("initModeBadge", () => {
    beforeEach(() => {
        document.body.innerHTML = `<span id="mode-badge" class="mode-badge"></span>`;
    });

    it("shows the human label for the mode", () => {
        initModeBadge("view");
        expect(document.getElementById("mode-badge")!.textContent).toBe("View");
    });

    it("adds a mode-specific class and an accessible description", () => {
        const badge = initModeBadge("static");
        expect(badge.classList.contains("mode-static")).toBe(true);
        expect(badge.getAttribute("aria-label")).toContain(MODE_INFO.static.description);
    });

    it("labels edit mode", () => {
        initModeBadge("edit");
        expect(document.getElementById("mode-badge")!.textContent).toBe("Edit");
    });
});

describe("MODE_INFO", () => {
    it("has a distinct label and description for every mode", () => {
        const modes = ["static", "view", "edit"] as const;
        const labels = modes.map((m) => MODE_INFO[m].label);
        expect(new Set(labels).size).toBe(modes.length);
        for (const mode of modes) {
            expect(MODE_INFO[mode].description.length).toBeGreaterThan(10);
        }
    });
});
