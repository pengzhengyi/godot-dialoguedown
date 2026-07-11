import { describe, it, expect, vi } from "vitest";
import { createMaximizeButton } from "./maximize-button";

describe("createMaximizeButton", () => {
    it("renders a button carrying both the expand and compress icons", () => {
        const button = createMaximizeButton(vi.fn());
        expect(button.tagName).toBe("BUTTON");
        expect(button.type).toBe("button");
        expect(button.classList.contains("maximize-button")).toBe(true);
        expect(button.querySelector(".icon-expand")).not.toBeNull();
        expect(button.querySelector(".icon-compress")).not.toBeNull();
    });

    it("gives the button an accessible label and tooltip", () => {
        const button = createMaximizeButton(vi.fn());
        expect(button.getAttribute("aria-label")).toBeTruthy();
        expect(button.title).toBeTruthy();
    });

    it("runs the toggle handler on click", () => {
        const onToggle = vi.fn();
        const button = createMaximizeButton(onToggle);
        button.click();
        expect(onToggle).toHaveBeenCalledOnce();
    });
});
