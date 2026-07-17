import { describe, it, expect, vi, afterEach } from "vitest";
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
    });

    it("shows the id with its @ sigil, exactly as a script references it", () => {
        const view = createConfigView(withFile());

        const idCell = view.querySelector(".config-speakers-table tbody td:nth-child(2)");
        expect(idCell?.textContent).toBe("@A");
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

    it("shows a maximize button only when a fullscreen toggle is provided", () => {
        expect(createConfigView(withFile()).querySelector(".config-controls")).toBeNull();

        const onToggleFullscreen = vi.fn();
        const view = createConfigView(withFile(), { onToggleFullscreen });
        const button = view.querySelector<HTMLButtonElement>(".config-controls .maximize-button");
        expect(button).not.toBeNull();

        button!.click();
        expect(onToggleFullscreen).toHaveBeenCalledOnce();
    });

    describe("click to copy", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        afterEach(() => writeText.mockClear());

        function clickCopy(selector: string): void {
            const view = createConfigView(withFile());
            view.querySelector<HTMLElement>(selector)!.dispatchEvent(
                new MouseEvent("click", { bubbles: true }),
            );
        }

        it("copies a speaker's name", () => {
            clickCopy(".config-speakers-table tbody td:nth-child(1)");
            expect(writeText).toHaveBeenCalledWith("Alice");
        });

        it("copies the @id", () => {
            clickCopy(".config-speakers-table tbody td:nth-child(2)");
            expect(writeText).toHaveBeenCalledWith("@A");
        });

        it("copies a tag chip's text", () => {
            clickCopy(".config-tag-reserved");
            expect(writeText).toHaveBeenCalledWith("##default");
        });
    });
});
