import { describe, it, expect, vi } from "vitest";
import { createModeToggle } from "./mode-toggle";

describe("createModeToggle", () => {
    it("renders View and Edit options and reflects the initial mode", () => {
        const toggle = createModeToggle("view", vi.fn());
        const modes = [...toggle.element.querySelectorAll(".mode-toggle-option")].map((b) =>
            b.getAttribute("data-mode"),
        );

        expect(modes).toEqual(["view", "edit"]);
        expect(pressed(toggle.element, "view")).toBe("true");
        expect(pressed(toggle.element, "edit")).toBe("false");
    });

    it("calls onSelect with the clicked mode", () => {
        const onSelect = vi.fn();
        const toggle = createModeToggle("view", onSelect);

        button(toggle.element, "edit").click();

        expect(onSelect).toHaveBeenCalledWith("edit");
    });

    it("reflect moves the pressed state to the given mode", () => {
        const toggle = createModeToggle("view", vi.fn());

        toggle.reflect("edit");

        expect(pressed(toggle.element, "edit")).toBe("true");
        expect(pressed(toggle.element, "view")).toBe("false");
    });

    it("setEnabled disables both options and marks the group when frozen", () => {
        const toggle = createModeToggle("view", vi.fn());

        toggle.setEnabled(false);

        expect(toggle.element.getAttribute("aria-disabled")).toBe("true");
        expect(button(toggle.element, "view").disabled).toBe(true);
        expect(button(toggle.element, "edit").disabled).toBe(true);
        expect(toggle.element.title).not.toBe("");

        toggle.setEnabled(true);

        expect(toggle.element.getAttribute("aria-disabled")).toBe("false");
        expect(button(toggle.element, "view").disabled).toBe(false);
        expect(button(toggle.element, "edit").disabled).toBe(false);
    });
});

function button(root: HTMLElement, mode: string): HTMLButtonElement {
    return root.querySelector(`[data-mode="${mode}"]`)!;
}

function pressed(root: HTMLElement, mode: string): string | null {
    return button(root, mode).getAttribute("aria-pressed");
}
