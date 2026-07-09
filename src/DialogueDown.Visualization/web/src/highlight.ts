import hljs from "highlight.js/lib/core";
import markdown from "highlight.js/lib/languages/markdown";

// Register only the Markdown grammar (the source is a Markdown-based dialogue
// script), keeping the bundle lean — the rest of highlight.js is tree-shaken out.
hljs.registerLanguage("markdown", markdown);

/**
 * Syntax-highlight a Markdown source string, returning HTML. highlight.js escapes
 * the input, so the result is safe to assign to `innerHTML`.
 */
export function highlightMarkdown(source: string): string {
    return hljs.highlight(source, { language: "markdown" }).value;
}
