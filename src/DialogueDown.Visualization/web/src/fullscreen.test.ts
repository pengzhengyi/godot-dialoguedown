import { describe, it, expect } from "vitest";
import { initFullscreen, MAXIMIZED_CLASS } from "./fullscreen";

/** A throwaway document so each controller's keydown listener stays isolated. */
function scratch(): { doc: Document; root: HTMLElement } {
    const doc = document.implementation.createHTMLDocument("fullscreen-test");
    return { doc, root: doc.body };
}

/** Dispatch a bubbling keydown from `target` (defaults to the document body). */
function press(
    doc: Document,
    key: string,
    init: KeyboardEventInit = {},
    target: EventTarget = doc.body,
): KeyboardEvent {
    const event = new KeyboardEvent("keydown", { key, bubbles: true, cancelable: true, ...init });
    target.dispatchEvent(event);
    return event;
}

describe("initFullscreen", () => {
    it("toggles the maximized class on the root", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        expect(fs.isMaximized()).toBe(false);
        fs.toggle();
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(true);
        expect(fs.isMaximized()).toBe(true);
        fs.toggle();
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(false);
    });

    it("exit leaves full screen and is a no-op when already minimized", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        fs.exit();
        expect(fs.isMaximized()).toBe(false);
        fs.toggle();
        fs.exit();
        expect(fs.isMaximized()).toBe(false);
    });

    it("toggles on the `f` key and prevents its default", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        const event = press(doc, "f");
        expect(fs.isMaximized()).toBe(true);
        expect(event.defaultPrevented).toBe(true);
        press(doc, "F"); // caps/shift still toggles
        expect(fs.isMaximized()).toBe(false);
    });

    it("ignores `f` with a modifier so browser/OS shortcuts are untouched", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        for (const mod of [{ ctrlKey: true }, { metaKey: true }, { altKey: true }]) {
            press(doc, "f", mod);
            expect(fs.isMaximized()).toBe(false);
        }
    });

    it("ignores `f` while typing in the editor or a form field", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const editor = doc.createElement("div");
        editor.className = "cm-editor";
        const content = doc.createElement("div");
        editor.appendChild(content);
        doc.body.appendChild(editor);
        press(doc, "f", {}, content);
        expect(fs.isMaximized()).toBe(false);

        const input = doc.createElement("input");
        doc.body.appendChild(input);
        press(doc, "f", {}, input);
        expect(fs.isMaximized()).toBe(false);
    });

    it("exits on Escape only while maximized, and yields otherwise", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const ignored = press(doc, "Escape");
        expect(ignored.defaultPrevented).toBe(false);

        fs.toggle();
        const handled = press(doc, "Escape");
        expect(fs.isMaximized()).toBe(false);
        expect(handled.defaultPrevented).toBe(true);
    });

    it("respects a key another handler already consumed", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const event = new KeyboardEvent("keydown", { key: "f", bubbles: true, cancelable: true });
        event.preventDefault();
        doc.body.dispatchEvent(event);
        expect(fs.isMaximized()).toBe(false);
    });

    it("reflects the pressed state on every maximize button", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const button = doc.createElement("button");
        button.className = "maximize-button";
        doc.body.appendChild(button);

        fs.toggle();
        expect(button.getAttribute("aria-pressed")).toBe("true");
        expect(button.title).toContain("Exit");

        fs.toggle();
        expect(button.getAttribute("aria-pressed")).toBe("false");
        expect(button.title).toContain("Full screen");
    });
});
