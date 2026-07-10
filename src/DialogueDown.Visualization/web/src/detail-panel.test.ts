import { describe, it, expect, beforeEach } from "vitest";
import { createDetailPanel, type DetailPanel } from "./detail-panel";
import { colorOf } from "./palette";
import type { DisplayNode } from "./model";

describe("createDetailPanel", () => {
    let panel: DetailPanel;
    let title: HTMLElement;
    let body: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        title = document.getElementById("detail-title")!;
        body = document.getElementById("detail-body")!;
        panel = createDetailPanel();
    });

    it("starts with a placeholder after clear", () => {
        panel.clear();
        expect(title.textContent).toBe("Node details");
        expect(body.textContent).toContain("Click any node");
    });

    it("shows a category color dot and the escaped label", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "Code <span>",
            category: "call",
            attributes: [],
        };
        panel.show(node);
        const dot = title.querySelector<HTMLElement>(".dot");
        expect(dot?.style.background).toBeTruthy();
        expect(title.textContent).toContain("Code <span>");
        expect(title.innerHTML).toContain("&lt;span&gt;");
        // The dot uses the category color (jsdom normalizes the hex, so compare via a probe element).
        const probe = document.createElement("span");
        probe.style.background = colorOf("call");
        expect(dot?.style.background).toBe(probe.style.background);
    });

    it("omits the dot when the node has no category", () => {
        panel.show({ id: "n1", label: "Text", attributes: [] });
        expect(title.querySelector(".dot")).toBeNull();
    });

    it("renders a table of attributes", () => {
        panel.show({
            id: "n1",
            label: "Heading",
            attributes: [
                { name: "level", value: "2" },
                { name: "text", value: "Scene" },
            ],
        });
        const rows = body.querySelectorAll("table tr");
        expect(rows).toHaveLength(2);
        expect(rows[0].querySelector("th")?.textContent).toBe("level");
        expect(rows[0].querySelector("td")?.textContent).toBe("2");
    });

    it("renders a source block and a Markdown preview when source is present", () => {
        panel.show({ id: "n1", label: "Heading", attributes: [], source: "# Scene" });
        expect(body.textContent).toContain("Source");
        expect(body.querySelector("pre code")?.textContent).toBe("# Scene");
        expect(body.querySelector(".preview")?.innerHTML).toContain("<h1>Scene</h1>");
    });

    it("omits the source section when there is no source", () => {
        panel.show({ id: "n1", label: "Text", attributes: [] });
        expect(body.textContent).not.toContain("Source");
        expect(body.querySelector("pre")).toBeNull();
    });

    it("escapes the source so it cannot inject markup", () => {
        panel.show({ id: "n1", label: "Text", attributes: [], source: "<script>x" });
        expect(body.querySelector("pre code")?.innerHTML).toContain("&lt;script&gt;");
    });
});
