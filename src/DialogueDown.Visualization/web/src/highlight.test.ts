import { describe, it, expect } from "vitest";
import { highlightMarkdown } from "./highlight";

describe("highlightMarkdown", () => {
    it("wraps Markdown tokens in highlight.js spans", () => {
        const html = highlightMarkdown("# Heading\n**bold**");
        expect(html).toContain('class="hljs-section"');
        expect(html).toContain('class="hljs-strong"');
    });

    it("escapes HTML so the source cannot inject markup", () => {
        expect(highlightMarkdown("<script>x")).toContain("&lt;script&gt;");
    });

    it("preserves the source text when read back as plain text", () => {
        const source = "# A\n\n- item";
        const div = document.createElement("div");
        div.innerHTML = highlightMarkdown(source);
        expect(div.textContent).toBe(source);
    });
});
