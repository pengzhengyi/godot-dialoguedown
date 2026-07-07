import { marked } from "marked";
import type { DisplayNode } from "./model";

/** Longest inline label/attribute drawn on a node before it is ellipsised. */
export const MAX_INLINE_TEXT = 30;

/** Escape a value for safe insertion into HTML. */
export function escapeHtml(value: string): string {
    return value.replace(
        /[&<>"']/g,
        (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[ch]!,
    );
}

/** Shorten a string to a maximum length with an ellipsis. */
export function ellipsize(text: string, max: number): string {
    return text.length > max ? text.slice(0, max - 1) + "…" : text;
}

/** A node's type name without any parenthetical detail ("Heading (H2)" -> "Heading"). */
export function baseLabel(label: string): string {
    return label.replace(/\s*\(.*\)\s*$/, "");
}

/** HTML for a node's hover tooltip: its label and full (untruncated) attributes. */
export function tooltipHtml(node: DisplayNode): string {
    const parts = [`<strong>${escapeHtml(node.label)}</strong>`];
    for (const attr of node.attributes) {
        parts.push(`<div>${escapeHtml(attr.name)}: ${escapeHtml(attr.value)}</div>`);
    }
    return parts.join("");
}

interface FrontMatterSplit {
    frontMatter: string | null;
    body: string;
}

/**
 * Split a leading YAML front matter block off a source string. marked has no
 * notion of front matter and would render `title:` + `---` as a heading, so we
 * peel it off and show it as metadata instead.
 */
export function splitFrontMatter(source: string): FrontMatterSplit {
    const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(source);
    if (match) {
        return { frontMatter: match[1], body: source.slice(match[0].length) };
    }
    return { frontMatter: null, body: source };
}

/** Render Markdown to HTML, handling a leading YAML front matter block. */
export function renderMarkdown(source: string): string {
    const { frontMatter, body } = splitFrontMatter(source);
    const head = frontMatter
        ? `<h4>Front matter</h4><pre class="frontmatter"><code>${escapeHtml(frontMatter)}</code></pre>`
        : "";
    return head + (marked.parse(body, { async: false }) as string);
}
