import {
    create,
    linkHorizontal,
    stratify,
    tree,
    zoom,
    zoomIdentity,
    zoomTransform,
    type EnterElement,
    type HierarchyPointLink,
    type HierarchyPointNode,
    type Selection,
} from "d3";
import type { DisplayEdge, DisplayNode, Stage } from "./model";
import type { CameraTransform } from "./graph-camera";
import { colorOf } from "./palette";
import { ellipsize, MAX_INLINE_TEXT, tooltipHtml } from "./text";
import { createLegend } from "./legend";
import { createZoomControls, ZOOM_STEP, type ZoomControls } from "./zoom-controls";

/** A laid-out hierarchy node augmented with collapse state (`_children`). */
type TreeNode = HierarchyPointNode<DisplayNode> & {
    _children?: TreeNode[];
    children?: TreeNode[];
};

export interface TreeView {
    svg: SVGSVGElement;
    legend: HTMLElement;
    controls: HTMLElement;
    handleKey(event: KeyboardEvent): void;
    clearSelection(): void;
    /**
     * Show the given camera and fold. A `null` camera uses the default (root-centered)
     * framing. Call after the tab becomes visible so the framing uses real dimensions.
     */
    applyView(camera: CameraTransform | null, fold: string[]): void;
}

/** Hooks that let the app remember and restore a graph's position across tabs. */
export interface TreeViewOptions {
    /**
     * The camera to apply on creation: a pinned override, the inherited shared
     * camera, or `null` for the default (root-centered) framing.
     */
    initialCamera?: CameraTransform | null;
    /** The collapsed node ids to restore on creation. */
    initialFold?: string[];
    /**
     * Fired when the camera changes; `byUser` is true for reader gestures (wheel,
     * drag, the zoom controls) and false for programmatic applies (a reveal, the
     * default framing).
     */
    onCameraChange?(transform: CameraTransform, byUser: boolean): void;
    /** Fired when the reader collapses or expands a node. */
    onFoldChange?(collapsed: string[]): void;
    /** Fired when the reader clicks Revert, so the caller can drop remembered state. */
    onRevert?(): void;
    /** Toggle the whole-window maximize mode (the zoom cluster's trailing button). */
    onToggleFullscreen?(): void;
}

const NAVIGATION_KEYS = ["ArrowRight", "ArrowLeft", "ArrowUp", "ArrowDown", "Enter", " "];

// Default framing: a readable 100% zoom with the root anchored near the left edge and
// vertically centered, so the reader starts at the root with its subtree filling the
// viewport rightward — rather than a whole-graph fit that shrinks large trees.
const DEFAULT_ZOOM = 1;
const ROOT_ANCHOR_X = 0.2;

/** Render one stage as an interactive, collapsible D3 tree with legend + zoom.
 *  `options` supply the initial camera/fold and hooks so the app can remember a
 *  graph's position across tab switches and hot-reloads. */
export function createTreeView(
    stage: Stage,
    onSelect: (node: DisplayNode) => void,
    options: TreeViewOptions = {},
): TreeView {
    const {
        initialCamera = null,
        initialFold = [],
        onCameraChange,
        onFoldChange,
        onRevert,
        onToggleFullscreen = () => {},
    } = options;
    const referenceEdges = stage.edges.filter((edge) => edge.kind === "Reference");
    const root = buildHierarchy(stage);
    root.each((node) => {
        (node as TreeNode)._children = (node as TreeNode).children;
    });

    let selected: TreeNode | null = null;
    const dimmed = new Set<string>();
    // Set while a control-driven (user) zoom is applied, so the zoom handler can tell
    // reader gestures from programmatic applies even when both lack a DOM sourceEvent.
    let userGesture = false;
    // Bumped by every applyView so a stale async default-framing retry (scheduled by an
    // earlier applyView, e.g. this tab's hidden construction) aborts instead of clobbering
    // a camera a later applyView (e.g. the reveal) has since applied.
    let viewToken = 0;

    const svg = create<SVGSVGElement>("svg").attr("class", "tree");
    const viewport = svg.append("g");
    const gLinks = viewport.append("g");
    const gReferences = viewport.append("g");
    const gNodes = viewport.append("g");

    const zoomBehavior = zoom<SVGSVGElement, undefined>()
        .scaleExtent([0.1, 3])
        // Use the container size as the extent so zoom centers correctly and does not
        // depend on the SVG's intrinsic size.
        .extent(() => {
            const size = viewportSize();
            return [
                [0, 0],
                [size.width, size.height],
            ] as [[number, number], [number, number]];
        })
        .on("zoom", (event) => {
            viewport.attr("transform", event.transform.toString());
            controls.setRatio(event.transform.k);
            const byUser = userGesture || Boolean(event.sourceEvent);
            const { k, x, y } = event.transform;
            onCameraChange?.({ k, x, y }, byUser);
        });
    svg.call(zoomBehavior);

    const controls: ZoomControls = createZoomControls({
        onZoomIn: () => userAction(() => svg.call(zoomBehavior.scaleBy, ZOOM_STEP)),
        onZoomOut: () => userAction(() => svg.call(zoomBehavior.scaleBy, 1 / ZOOM_STEP)),
        onSetZoom: (percent) =>
            userAction(() => svg.call(zoomBehavior.scaleTo, clampScale(percent / 100))),
        onRevert: () => revert(),
        onToggleFullscreen,
    });

    const legend = createLegend(stage, {
        onToggle: (category, isDimmed) => {
            if (isDimmed) dimmed.add(category);
            else dimmed.delete(category);
            applyCategoryFilter();
        },
        onHover: (category) => highlightCategory(category),
        onLeave: () => clearHighlight(),
    });

    const layout = tree<DisplayNode>().nodeSize([62, 220]);
    const diagonal = linkHorizontal<
        HierarchyPointLink<DisplayNode>,
        HierarchyPointNode<DisplayNode>
    >()
        .x((node) => node.y)
        .y((node) => node.x);

    applyView(initialCamera, initialFold);

    return {
        svg: svg.node()!,
        legend,
        controls: controls.element,
        handleKey,
        clearSelection: () => {
            selected = null;
            applySelection();
        },
        applyView,
    };

    /* --- hierarchy --- */

    function buildHierarchy(stage: Stage): TreeNode {
        const parentOf = new Map<string, string>();
        for (const edge of stage.edges) {
            if (edge.kind !== "Reference") parentOf.set(edge.toId, edge.fromId);
        }
        return stratify<DisplayNode>()
            .id((node) => node.id)
            .parentId((node) => parentOf.get(node.id) ?? null)(stage.nodes) as unknown as TreeNode;
    }

    /* --- selection, filter, highlight --- */

    function select(node: TreeNode): void {
        selected = node;
        applySelection();
        onSelect(node.data);
    }

    function applySelection(): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("selected", (d) => d === selected);
    }

    function applyCategoryFilter(): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("dimmed", (d) => Boolean(d.data.category && dimmed.has(d.data.category)));
    }

    function highlightCategory(category: string): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("highlight", (d) => d.data.category === category);
    }

    function clearHighlight(): void {
        gNodes.selectAll<SVGGElement, TreeNode>("g.node").classed("highlight", false);
    }

    /* --- collapse / expand --- */

    function toggle(node: TreeNode): void {
        node.children = node.children ? undefined : node._children;
        update();
        onFoldChange?.(collapsedIds());
    }

    function expand(node: TreeNode): void {
        if (!node.children && node._children) {
            node.children = node._children;
            update();
            onFoldChange?.(collapsedIds());
        }
    }

    /* --- keyboard navigation --- */

    function handleKey(event: KeyboardEvent): void {
        if (!NAVIGATION_KEYS.includes(event.key)) return;
        event.preventDefault();

        if (!selected) {
            select(root);
            scheduleDefaultView(++viewToken);
            return;
        }
        if (event.key === "Enter" || event.key === " ") {
            toggle(selected);
            applySelection();
            return;
        }
        const next = nextNode(event.key, selected);
        if (next) {
            select(next);
            centerOn(next);
        }
    }

    function nextNode(key: string, node: TreeNode): TreeNode | null {
        if (key === "ArrowRight") {
            expand(node);
            return node.children ? node.children[0] : null;
        }
        if (key === "ArrowLeft") return (node.parent as TreeNode | null) ?? null;
        if (key === "ArrowDown") return sibling(node, 1);
        if (key === "ArrowUp") return sibling(node, -1);
        return null;
    }

    function sibling(node: TreeNode, offset: number): TreeNode | null {
        const siblings = node.parent?.children as TreeNode[] | undefined;
        if (!siblings) return null;
        return siblings[siblings.indexOf(node) + offset] ?? null;
    }

    /* --- rendering --- */

    function update(): void {
        layout(root);
        const nodes = root.descendants() as TreeNode[];
        const positionById = new Map(nodes.map((node) => [node.data.id, node]));

        gLinks
            .selectAll<SVGPathElement, HierarchyPointLink<DisplayNode>>("path.link")
            .data(root.links(), (link) => (link.target as TreeNode).data.id)
            .join("path")
            .attr("class", "link")
            .attr("d", diagonal);

        gReferences
            .selectAll<SVGPathElement, DisplayEdge>("path.reference")
            .data(
                referenceEdges.filter(
                    (edge) => positionById.has(edge.fromId) && positionById.has(edge.toId),
                ),
                (edge) => `${edge.fromId}->${edge.toId}`,
            )
            .join("path")
            .attr("class", "link reference")
            .attr("d", (edge) =>
                diagonal({
                    source: positionById.get(edge.fromId)!,
                    target: positionById.get(edge.toId)!,
                }),
            );

        const node = gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .data(nodes, (datum) => datum.data.id);
        node.exit().remove();
        appendEnteringNodes(node.enter());

        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .attr("transform", (d) => `translate(${d.y},${d.x})`)
            .classed("collapsed", (d) => !d.children && Boolean(d._children));

        applySelection();
        applyCategoryFilter();
    }

    function appendEnteringNodes(
        enter: Selection<EnterElement, TreeNode, SVGGElement, unknown>,
    ): void {
        const group = enter
            .append("g")
            .attr("class", "node")
            .attr("data-tip", (d) => tooltipHtml(d.data));

        group
            .append("circle")
            .attr("r", 5)
            .style("fill", (d) => colorOf(d.data.category))
            .on("click", (_event, d) => {
                toggle(d);
                select(d);
            });

        group
            .append("text")
            .attr("class", "label")
            .attr("dy", "0.32em")
            .attr("x", 12)
            .text((d) => d.data.label);

        group.each(function (d) {
            d.data.attributes.forEach((attr, i) => {
                const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
                text.setAttribute("class", "attr");
                text.setAttribute("x", "12");
                text.setAttribute("dy", String(15 + i * 12));
                text.textContent = ellipsize(`${attr.name}: ${attr.value}`, MAX_INLINE_TEXT);
                this.appendChild(text);
            });
        });

        // A generous transparent hit area behind the label and attributes, so the
        // whole node block is clickable to inspect. Size is estimated from the
        // (ellipsised) text, so it never depends on getBBox.
        group.each(function (d) {
            const lines = [
                d.data.label,
                ...d.data.attributes.map((a) => `${a.name}: ${a.value}`),
            ].map((line) => ellipsize(line, MAX_INLINE_TEXT));
            const longest = lines.reduce((max, line) => Math.max(max, line.length), 0);
            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            rect.setAttribute("class", "hit");
            rect.setAttribute("x", "-8");
            rect.setAttribute("y", "-12");
            rect.setAttribute("width", String(12 + longest * 6.5 + 10));
            rect.setAttribute("height", String(20 + d.data.attributes.length * 12));
            rect.addEventListener("click", () => select(d));
            this.insertBefore(rect, this.firstChild);
        });
    }

    /* --- camera --- */

    /** Run a reader-initiated camera change so the zoom handler pins it (not just notes it). */
    function userAction(change: () => void): void {
        userGesture = true;
        try {
            change();
        } finally {
            userGesture = false;
        }
    }

    /** Show a camera and fold; a `null` camera uses the default (root-centered) framing. */
    function applyView(camera: CameraTransform | null, fold: string[]): void {
        const token = ++viewToken;
        setFold(fold);
        update();
        if (camera) applyTransform(camera);
        else scheduleDefaultView(token);
    }

    /** Revert this graph to defaults: drop remembered state, expand all, re-frame. */
    function revert(): void {
        onRevert?.();
        applyView(null, []);
    }

    /**
     * Frame the default (root-centered) view once the container has a real size. A
     * just-shown or hidden tab reads zero until it lays out, so retry next frame (capped
     * so a never-shown tab does not loop forever). The `token` aborts the retry if a later
     * applyView has superseded this one.
     */
    function scheduleDefaultView(token: number, attempt = 0): void {
        if (token !== viewToken) return;
        const parent = svg.node()?.parentElement;
        const width = parent?.clientWidth ?? 0;
        const height = parent?.clientHeight ?? 0;
        if (!width || !height) {
            if (attempt < 30) requestAnimationFrame(() => scheduleDefaultView(token, attempt + 1));
            return;
        }
        const rootX = (root as TreeNode).x ?? 0; // vertical position after layout
        const rootY = (root as TreeNode).y ?? 0; // horizontal position (0 at the root)
        const tx = width * ROOT_ANCHOR_X - DEFAULT_ZOOM * rootY;
        const ty = height / 2 - DEFAULT_ZOOM * rootX;
        applyTransform({ k: DEFAULT_ZOOM, x: tx, y: ty });
    }

    function centerOn(node: TreeNode): void {
        try {
            const size = viewportSize();
            const transform = zoomTransform(svg.node()!);
            const tx = size.width / 2 - node.y * transform.k;
            const ty = size.height / 2 - node.x * transform.k;
            svg.call(zoomBehavior.transform, zoomIdentity.translate(tx, ty).scale(transform.k));
        } catch {
            /* centring is optional */
        }
    }

    function clampScale(scale: number): number {
        return Math.max(0.1, Math.min(3, scale));
    }

    function applyTransform(transform: CameraTransform): void {
        try {
            svg.call(
                zoomBehavior.transform,
                zoomIdentity.translate(transform.x, transform.y).scale(transform.k),
            );
        } catch {
            /* leave the tree at its default position */
        }
    }

    /* --- fold --- */

    /** The ids of nodes that are collapsed (have hidden children) and currently visible. */
    function collapsedIds(): string[] {
        const ids: string[] = [];
        root.each((node) => {
            const treeNode = node as TreeNode;
            if (!treeNode.children && treeNode._children) ids.push(treeNode.data.id);
        });
        return ids;
    }

    /** Reset every node to expanded, then collapse exactly the ids that still exist. */
    function setFold(collapsed: readonly string[]): void {
        const wanted = new Set(collapsed);
        eachOriginal(root, (node) => {
            node.children = node._children;
            if (wanted.has(node.data.id)) node.children = undefined;
        });
    }

    /** Visit every node of the original (pre-collapse) hierarchy, top-down. */
    function eachOriginal(node: TreeNode, visit: (node: TreeNode) => void): void {
        visit(node);
        for (const child of node._children ?? []) eachOriginal(child, visit);
    }

    function viewportSize(): { width: number; height: number } {
        const parent = svg.node()?.parentElement;
        return {
            width: parent?.clientWidth || 800,
            height: parent?.clientHeight || 600,
        };
    }
}
