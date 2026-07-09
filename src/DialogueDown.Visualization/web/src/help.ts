/** Which tab's help to show: the Source tab or a stage graph tab. */
export type HelpContext = "source" | "graph";

const SOURCE_HELP = `
  <p><strong>Source &amp; preview.</strong> The left pane is the document as written;
     the right pane is a live Markdown preview.</p>
  <p><strong>Preview links</strong> jump to their headings within the preview.</p>
  <p><strong>Drag the divider</strong> between the panes to re-proportion them.</p>
`;

const GRAPH_HELP = `
  <p><strong>Click a node</strong> (its label or details) to inspect the source it
     was produced from and a rendered preview.</p>
  <p><strong>Click a node's circle</strong> to collapse or expand its children.</p>
  <p><strong>Drag</strong> to pan, <strong>scroll</strong> to zoom, and
     <strong>drag the divider</strong> to resize the detail panel.</p>
  <p><strong>Zoom controls</strong> (bottom-right): <kbd>+</kbd> / <kbd>−</kbd> to
     zoom, and click the percentage to reset the view.</p>
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
