import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { splitPath, copyToClipboard, initPathDisplay } from "./path-display";

describe("splitPath", () => {
    it("splits a POSIX path into directory and filename", () => {
        expect(splitPath("/home/alice/scene.dialogue.md")).toEqual({
            head: "/home/alice",
            tail: "/scene.dialogue.md",
        });
    });

    it("splits a Windows path", () => {
        expect(splitPath("C:\\docs\\scene.dialogue.md")).toEqual({
            head: "C:\\docs",
            tail: "\\scene.dialogue.md",
        });
    });

    it("returns the whole string as the tail when there is no separator", () => {
        expect(splitPath("scene.dialogue.md")).toEqual({ head: "", tail: "scene.dialogue.md" });
    });
});

describe("copyToClipboard", () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("uses the Clipboard API when available", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });

        await copyToClipboard("/path/x.md");

        expect(writeText).toHaveBeenCalledWith("/path/x.md");
    });

    it("falls back to execCommand when the Clipboard API is missing", async () => {
        vi.stubGlobal("navigator", {});
        const exec = vi.fn();
        (document as unknown as { execCommand: unknown }).execCommand = exec;

        await copyToClipboard("/path/x.md");

        expect(exec).toHaveBeenCalledWith("copy");
    });
});

describe("initPathDisplay", () => {
    beforeEach(() => {
        document.body.innerHTML = `
      <button id="doc-path" hidden>
        <span class="path-head"></span><span class="path-tail"></span>
      </button>`;
    });

    it("fills the head and tail and reveals the button", () => {
        const button = initPathDisplay("/home/alice/scene.dialogue.md")!;
        expect(button.hidden).toBe(false);
        expect(button.querySelector(".path-head")!.textContent).toBe("/home/alice");
        expect(button.querySelector(".path-tail")!.textContent).toBe("/scene.dialogue.md");
    });

    it("stays hidden when there is no path", () => {
        const button = initPathDisplay(undefined)!;
        expect(button.hidden).toBe(true);
    });

    it("returns null when the path element is absent", () => {
        document.body.innerHTML = "";
        expect(initPathDisplay("/x.md")).toBeNull();
    });

    it("copies the full path when clicked", async () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal("navigator", { clipboard: { writeText } });

        const button = initPathDisplay("/home/alice/scene.dialogue.md")!;
        button.click();
        await Promise.resolve();

        expect(writeText).toHaveBeenCalledWith("/home/alice/scene.dialogue.md");
        vi.restoreAllMocks();
    });
});
