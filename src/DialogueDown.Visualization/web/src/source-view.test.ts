import { describe, it, expect, beforeEach } from "vitest";
import { createSourceView } from "./source-view";

const SOURCE = `---
title: Demo
---
# Scene

Go to [Market](#market).

## Market

Hello there.`;

describe("createSourceView", () => {
    let view: HTMLElement;

    beforeEach(() => {
        view = createSourceView(SOURCE);
    });

    it("renders a split of a source pane, a divider, and a preview pane", () => {
        expect(view.classList.contains("source-view")).toBe(true);
        expect(view.querySelector(".source-pane")).not.toBeNull();
        expect(view.querySelector(".source-divider")).not.toBeNull();
        expect(view.querySelector(".source-preview")).not.toBeNull();
    });

    it("shows the whole document verbatim in the source pane", () => {
        expect(view.querySelector(".source-pane pre code")?.textContent).toBe(SOURCE);
    });

    it("renders the preview with heading ids that match in-document anchor links", () => {
        const preview = view.querySelector(".source-preview")!;
        expect(preview.querySelector("#market")).not.toBeNull();
        expect(preview.querySelector('a[href="#market"]')).not.toBeNull();
    });

    it("shows front matter as metadata rather than a heading", () => {
        expect(view.querySelector(".source-preview .frontmatter")).not.toBeNull();
    });

    it("exposes the divider as a separator for assistive tech", () => {
        const divider = view.querySelector(".source-divider")!;
        expect(divider.getAttribute("role")).toBe("separator");
        expect(divider.getAttribute("aria-orientation")).toBe("vertical");
    });
});

describe("source divider drag", () => {
    function widthOf(view: HTMLElement, width: number): void {
        view.getBoundingClientRect = () =>
            ({
                left: 0,
                width,
                top: 0,
                right: width,
                bottom: 0,
                height: 0,
                x: 0,
                y: 0,
                toJSON() {},
            }) as DOMRect;
    }

    it("re-proportions the source pane while dragging, clamped to [20%, 80%]", () => {
        const view = createSourceView("# x");
        widthOf(view, 1000);
        const pane = view.querySelector<HTMLElement>(".source-pane")!;
        const divider = view.querySelector<HTMLElement>(".source-divider")!;

        divider.dispatchEvent(new MouseEvent("mousedown"));
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 300 }));
        expect(pane.style.flexBasis).toBe("30%");
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 50 })); // below min
        expect(pane.style.flexBasis).toBe("20%");
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 970 })); // above max
        expect(pane.style.flexBasis).toBe("80%");
    });

    it("stops resizing after the drag ends", () => {
        const view = createSourceView("# x");
        widthOf(view, 1000);
        const pane = view.querySelector<HTMLElement>(".source-pane")!;
        const divider = view.querySelector<HTMLElement>(".source-divider")!;

        divider.dispatchEvent(new MouseEvent("mousedown"));
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 400 }));
        expect(pane.style.flexBasis).toBe("40%");
        document.dispatchEvent(new MouseEvent("mouseup"));
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 600 }));
        expect(pane.style.flexBasis).toBe("40%");
    });

    it("toggles text selection off during a drag and back on afterwards", () => {
        const view = createSourceView("# x");
        const divider = view.querySelector<HTMLElement>(".source-divider")!;
        divider.dispatchEvent(new MouseEvent("mousedown"));
        expect(document.body.style.userSelect).toBe("none");
        document.dispatchEvent(new MouseEvent("mouseup"));
        expect(document.body.style.userSelect).toBe("");
    });

    it("ignores a drag before the container has a measurable width", () => {
        const view = createSourceView("# x"); // no width mock: jsdom reports width 0
        const pane = view.querySelector<HTMLElement>(".source-pane")!;
        const divider = view.querySelector<HTMLElement>(".source-divider")!;
        divider.dispatchEvent(new MouseEvent("mousedown"));
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 300 }));
        expect(pane.style.flexBasis).toBe("");
    });
});
