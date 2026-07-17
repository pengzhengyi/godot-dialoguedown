import { describe, it, expect } from "vitest";
import { createConfigView } from "./config-view";
import type { ConfigReport } from "./model";

/** A configured-from-file report with one speaker carrying a custom and a reserved tag. */
function withFile(): ConfigReport {
    return {
        file: { path: "/proj/dialogue.toml", source: '[[speakers]]\nname = "Alice"\n' },
        speakers: [
            {
                name: "Alice",
                id: "A",
                tags: [
                    { name: "role", value: "guide", reserved: false },
                    { name: "default", reserved: true },
                ],
            },
        ],
    };
}

describe("createConfigView", () => {
    it("shows the TOML source in a read-only editor and the speakers table", () => {
        const view = createConfigView(withFile());

        const editor = view.querySelector(".config-source .cm-editor");
        expect(editor).not.toBeNull();
        expect(view.querySelector(".config-source")?.textContent).toContain("Alice");

        const rows = view.querySelectorAll(".config-speakers-table tbody tr");
        expect(rows).toHaveLength(1);
        expect(rows[0].textContent).toContain("Alice");
        expect(rows[0].textContent).toContain("A");
    });

    it("colors reserved and custom tags apart with distinct chip classes", () => {
        const view = createConfigView(withFile());

        const custom = view.querySelector(".config-tag-custom");
        const reserved = view.querySelector(".config-tag-reserved");
        expect(custom?.textContent).toBe("#role=guide");
        expect(reserved?.textContent).toBe("##default");
    });

    it("shows a friendly explanation and no editor when there is no config file", () => {
        const view = createConfigView({ speakers: [] });

        expect(view.querySelector(".config-source .cm-editor")).toBeNull();
        expect(view.querySelector(".config-empty-state")?.textContent).toContain("dialogue.toml");
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });

    it("shows an empty-speakers note when a file has no configured speakers", () => {
        const view = createConfigView({
            file: { path: "/p/dialogue.toml", source: "# empty\n" },
            speakers: [],
        });

        expect(view.querySelector(".config-source .cm-editor")).not.toBeNull();
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });
});
