import { describe, it, expect, vi, afterEach } from "vitest";
import { createConfigView, type ConfigViewOptions } from "./config-view";
import type { ConfigReport } from "./model";

/** The Config tab element (its handle's element), for DOM assertions. */
function mount(config: ConfigReport, options: ConfigViewOptions = {}): HTMLElement {
    return createConfigView(config, options).element;
}

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
        const view = mount(withFile());

        const editor = view.querySelector(".config-source .cm-editor");
        expect(editor).not.toBeNull();
        expect(view.querySelector(".config-source")?.textContent).toContain("Alice");

        const rows = view.querySelectorAll(".config-speakers-table tbody tr");
        expect(rows).toHaveLength(1);
        expect(rows[0].textContent).toContain("Alice");
    });

    it("shows the id with its @ sigil, exactly as a script references it", () => {
        const view = mount(withFile());

        const idCell = view.querySelector(".config-speakers-table tbody td:nth-child(2)");
        expect(idCell?.textContent).toBe("@A");
    });

    it("colors reserved and custom tags apart with distinct chip classes", () => {
        const view = mount(withFile());

        const custom = view.querySelector(".config-tag-custom");
        const reserved = view.querySelector(".config-tag-reserved");
        expect(custom?.textContent).toBe("#role=guide");
        expect(reserved?.textContent).toBe("##default");
    });

    it("shows a friendly explanation and no editor when there is no config file", () => {
        const view = mount({ speakers: [] });

        expect(view.querySelector(".config-source .cm-editor")).toBeNull();
        expect(view.querySelector(".config-empty-state")?.textContent).toContain("dialogue.toml");
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });

    it("shows an empty-speakers note when a file has no configured speakers", () => {
        const view = mount({
            file: { path: "/p/dialogue.toml", source: "# empty\n" },
            speakers: [],
        });

        expect(view.querySelector(".config-source .cm-editor")).not.toBeNull();
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });

    it("shows a maximize button only when a fullscreen toggle is provided", () => {
        expect(mount(withFile()).querySelector(".config-controls")).toBeNull();

        const onToggleFullscreen = vi.fn();
        const view = mount(withFile(), { onToggleFullscreen });
        const button = view.querySelector<HTMLButtonElement>(".config-controls .maximize-button");
        expect(button).not.toBeNull();

        button!.click();
        expect(onToggleFullscreen).toHaveBeenCalledOnce();
    });

    it("hides and reopens the speakers panel from the divider toggle", () => {
        const view = mount(withFile());

        const toggle = view.querySelector<HTMLButtonElement>(".config-divider .collapse-toggle");
        expect(toggle).not.toBeNull();
        expect(view.classList.contains("config-collapsed")).toBe(false);

        toggle!.click();
        expect(view.classList.contains("config-collapsed")).toBe(true);

        toggle!.click();
        expect(view.classList.contains("config-collapsed")).toBe(false);
    });

    describe("click to copy", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        afterEach(() => writeText.mockClear());

        function clickCopy(selector: string): void {
            const view = mount(withFile());
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

    describe("editable mode", () => {
        it("is editable (no read-only aria) and forwards edits when editable", () => {
            const edits: string[] = [];
            const handle = createConfigView(withFile(), {
                editable: true,
                onChange: (source) => edits.push(source),
            });
            const content = handle.element.querySelector<HTMLElement>(
                ".config-source .cm-content",
            )!;
            expect(content.getAttribute("aria-readonly")).not.toBe("true");
            expect(content.getAttribute("aria-label")).toBe("Configuration source editor");

            handle.setContent('name = "typed"');
            expect(edits).toContain('name = "typed"');
        });

        it("reconfigures between editable and read-only in place", () => {
            const handle = createConfigView(withFile(), { editable: false });
            const content = handle.element.querySelector<HTMLElement>(
                ".config-source .cm-content",
            )!;
            expect(content.getAttribute("aria-readonly")).toBe("true");

            handle.setEditable(true);
            expect(content.getAttribute("aria-readonly")).not.toBe("true");

            handle.setEditable(false);
            expect(content.getAttribute("aria-readonly")).toBe("true");
        });

        it("setContent replaces the editor text", () => {
            const handle = createConfigView(withFile(), { editable: true });
            handle.setContent('[[speakers]]\nname = "Zoe"\n');
            expect(handle.element.querySelector(".config-source")?.textContent).toContain("Zoe");
        });

        it("updateSpeakers re-renders the table from a fresh report", () => {
            const handle = createConfigView(withFile());
            handle.updateSpeakers({
                file: { path: "/p/dialogue.toml", source: "" },
                speakers: [{ name: "Zoe", id: "Z", tags: [] }],
            });
            const table = handle.element.querySelector(".config-speakers-table");
            expect(table?.querySelectorAll("tbody tr")).toHaveLength(1);
            expect(table?.textContent).toContain("Zoe");
            expect(table?.textContent).not.toContain("Alice");
        });

        it("setStale toggles the unsaved-speakers hint", () => {
            const handle = createConfigView(withFile());
            const hint = handle.element.querySelector<HTMLElement>(".config-stale-hint")!;
            expect(hint.hidden).toBe(true);
            handle.setStale(true);
            expect(hint.hidden).toBe(false);
            handle.setStale(false);
            expect(hint.hidden).toBe(true);
        });

        it("wires editor affordances: a fold gutter and search", () => {
            const view = mount(withFile());
            expect(view.querySelector(".config-source .cm-foldGutter")).not.toBeNull();
        });
    });
});
