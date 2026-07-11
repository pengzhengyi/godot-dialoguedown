import { describe, it, expect, beforeEach } from "vitest";
import { initResizer } from "./resizer";

function drag(clientX: number): void {
    document.dispatchEvent(new MouseEvent("mousemove", { clientX }));
}

describe("initResizer", () => {
    let detail: HTMLElement;
    let resizer: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<div id="resizer"></div><aside id="detail"></aside>`;
        resizer = document.getElementById("resizer")!;
        detail = document.getElementById("detail")!;
        initResizer();
    });

    it("resizes the detail panel while dragging (width = innerWidth − clientX)", () => {
        resizer.dispatchEvent(new MouseEvent("mousedown"));
        drag(window.innerWidth - 500);
        expect(detail.style.flexBasis).toBe("500px");
    });

    it("clamps the width to the [280, 760] range", () => {
        resizer.dispatchEvent(new MouseEvent("mousedown"));
        drag(window.innerWidth - 5000);
        expect(detail.style.flexBasis).toBe("760px");
        drag(window.innerWidth - 10);
        expect(detail.style.flexBasis).toBe("280px");
    });

    it("measures the detail's right edge so content padding doesn't offset the divider", () => {
        // The content area is padded, so the panel's right edge is inset from the window.
        detail.getBoundingClientRect = () => ({ right: 1000 }) as DOMRect;
        resizer.dispatchEvent(new MouseEvent("mousedown"));
        drag(600);
        expect(detail.style.flexBasis).toBe("400px"); // 1000 − 600, not innerWidth − 600
    });

    it("does nothing until a drag starts and after it ends", () => {
        drag(window.innerWidth - 400);
        expect(detail.style.flexBasis).toBe("");

        resizer.dispatchEvent(new MouseEvent("mousedown"));
        drag(window.innerWidth - 400);
        expect(detail.style.flexBasis).toBe("400px");

        document.dispatchEvent(new MouseEvent("mouseup"));
        drag(window.innerWidth - 600);
        expect(detail.style.flexBasis).toBe("400px");
    });

    it("toggles text selection off during a drag and back on afterwards", () => {
        resizer.dispatchEvent(new MouseEvent("mousedown"));
        expect(document.body.style.userSelect).toBe("none");
        document.dispatchEvent(new MouseEvent("mouseup"));
        expect(document.body.style.userSelect).toBe("");
    });

    it("returns quietly when the resizer or detail element is absent", () => {
        document.body.innerHTML = "";
        expect(() => initResizer()).not.toThrow();
    });
});
