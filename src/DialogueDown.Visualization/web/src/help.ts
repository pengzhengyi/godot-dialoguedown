/** Which tab's help to show: the Source tab or a stage graph tab. */
export type HelpContext = "source" | "graph";

const SOURCE_HELP = `
  <p><strong>Source &amp; preview.</strong> The left pane is the document as written;
     the right pane is a live Markdown preview.</p>
  <p><strong>Preview links</strong> jump to their headings within the preview.</p>
  <p><strong>Drag the divider</strong> between the panes to re-proportion them.</p>
  <p><strong>Editor.</strong> Find with <kbd>⌘/Ctrl-F</kbd> and fold a section from the
     gutter arrow. In Live Edit, format the selection with <kbd>⌘/Ctrl-B</kbd> (bold),
     <kbd>⌘/Ctrl-I</kbd> (italic) and <kbd>⌘/Ctrl-K</kbd> (link), or type <kbd>*</kbd>
     around a selection to emphasize it. To learn more about how to use the editor,
     check out <a href="https://codemirror.net/" target="_blank" rel="noopener noreferrer">CodeMirror</a>.</p>
  <p><strong>Full screen</strong> (bottom-right ⤢, or press <kbd>f</kbd> outside the
     editor): fill the window with the source and preview; <kbd>f</kbd> or <kbd>Esc</kbd>
     to leave.</p>
`;

const GRAPH_HELP = `
  <p><strong>Click a node</strong> (its label or details) to inspect the source it
     was produced from and a rendered preview.</p>
  <p><strong>Click a node's circle</strong> to collapse or expand its children.</p>
  <p><strong>Drag</strong> to pan, <strong>scroll</strong> to zoom, and
     <strong>drag the divider</strong> to resize the detail panel.</p>
  <p><strong>Zoom controls</strong> (bottom-right): <kbd>+</kbd> / <kbd>−</kbd> to
     zoom, and click the percentage to reset the view.</p>
  <p><strong>Full screen</strong> (the bottom-right ⤢ button, or press <kbd>f</kbd>):
     fill the window with the graph; <kbd>f</kbd> or <kbd>Esc</kbd> to leave.</p>
  <p><strong>Hover a legend entry</strong> (top-right) to highlight its nodes;
     <strong>click</strong> it to dim or show that type. The count shows how many
     are present.</p>
  <p><strong>Arrow keys</strong> move the selection: <kbd>→</kbd> first child,
     <kbd>←</kbd> parent, <kbd>↑</kbd>/<kbd>↓</kbd> siblings; <kbd>Enter</kbd> or
     <kbd>Space</kbd> collapses or expands.</p>
`;

const SUMMARY: Record<HelpContext, string> = {
    source: "Using the Source tab",
    graph: "Using the graph",
};

const CONTENT: Record<HelpContext, string> = {
    source: SOURCE_HELP,
    graph: GRAPH_HELP,
};

/**
 * Show help relevant to the active tab: the Source tab explains the source and
 * preview panes, a graph tab explains graph navigation. Keeps the footer help
 * focused on what the reader is actually looking at.
 */
export function setHelp(context: HelpContext): void {
    const summary = document.getElementById("help-summary");
    const content = document.getElementById("help-content");
    if (summary) summary.textContent = SUMMARY[context];
    if (content) content.innerHTML = CONTENT[context];
}

/**
 * Wire the footer's "How to use" disclosure: the toggle stays on the status line, and
 * clicking it shows or hides the shortcut panel below the status bar (full width).
 */
export function initHelpToggle(): void {
    const toggle = document.getElementById("help-toggle");
    const content = document.getElementById("help-content");
    if (!toggle || !content) return;
    toggle.addEventListener("click", () => {
        const open = toggle.getAttribute("aria-expanded") === "true";
        toggle.setAttribute("aria-expanded", String(!open));
        content.hidden = open;
    });
}
