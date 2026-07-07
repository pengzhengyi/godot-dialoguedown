import { describe, it, expect, vi } from "vitest";
import { createZoomControls, ZOOM_STEP, type ZoomHandlers } from "./zoom-controls";

function setup() {
    const handlers: ZoomHandlers = { onZoomIn: vi.fn(), onZoomOut: vi.fn(), onReset: vi.fn() };
    const controls = createZoomControls(handlers);
    const buttons = controls.element.querySelectorAll<HTMLButtonElement>("button");
    return { handlers, controls, buttons };
}

describe("createZoomControls", () => {
    it("renders a − / ratio / + widget", () => {
        const { buttons } = setup();
        expect([...buttons].map((b) => b.textContent)).toEqual(["−", "100%", "+"]);
    });

    it("gives every button an accessible label", () => {
        const { buttons } = setup();
        for (const button of buttons) {
            expect(button.getAttribute("aria-label")).toBeTruthy();
        }
    });

    it("wires each button to its handler", () => {
        const { handlers, buttons } = setup();
        const [minus, ratio, plus] = buttons;
        minus.click();
        ratio.click();
        plus.click();
        expect(handlers.onZoomOut).toHaveBeenCalledOnce();
        expect(handlers.onReset).toHaveBeenCalledOnce();
        expect(handlers.onZoomIn).toHaveBeenCalledOnce();
    });

    it("reflects the current scale as a rounded percentage", () => {
        const { controls, buttons } = setup();
        controls.setRatio(0.85);
        expect(buttons[1].textContent).toBe("85%");
        controls.setRatio(1.234);
        expect(buttons[1].textContent).toBe("123%");
    });

    it("exposes a sensible zoom step", () => {
        expect(ZOOM_STEP).toBeGreaterThan(1);
    });
});
