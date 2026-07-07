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
}

const NAVIGATION_KEYS = ["ArrowRight", "ArrowLeft", "ArrowUp", "ArrowDown", "Enter", " "];

/** Render one stage as an interactive, collapsible D3 tree with legend + zoom. */
export function createTreeView(stage: Stage, onSelect: (node: DisplayNode) => void): TreeView {
    const referenceEdges = stage.edges.filter((edge) => edge.kind === "Reference");
    const root = buildHierarchy(stage);
    root.each((node) => {
        (node as TreeNode)._children = (node as TreeNode).children;
    });

    let selected: TreeNode | null = null;
    const dimmed = new Set<string>();

    const svg = create<SVGSVGElement>("svg").attr("class", "tree");
    const viewport = svg.append("g");
    const gLinks = viewport.append("g");
    const gReferences = viewport.append("g");
    const gNodes = viewport.append("g");

    const zoomBehavior = zoom<SVGSVGElement, undefined>()
        .scaleExtent([0.1, 3])
        // Use the container size as the extent so zoom centres correctly and does not
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
        });
    svg.call(zoomBehavior);

    const controls: ZoomControls = createZoomControls({
        onZoomIn: () => svg.call(zoomBehavior.scaleBy, ZOOM_STEP),
        onZoomOut: () => svg.call(zoomBehavior.scaleBy, 1 / ZOOM_STEP),
        onReset: () => applyFit(),
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

    update();
    fitToViewport();

    return {
        svg: svg.node()!,
        legend,
        controls: controls.element,
        handleKey,
        clearSelection: () => {
            selected = null;
            applySelection();
        },
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
    }

    function expand(node: TreeNode): void {
        if (!node.children && node._children) {
            node.children = node._children;
            update();
        }
    }

    /* --- keyboard navigation --- */

    function handleKey(event: KeyboardEvent): void {
        if (!NAVIGATION_KEYS.includes(event.key)) return;
        event.preventDefault();

        if (!selected) {
            select(root);
            fitToViewport();
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

    function fitToViewport(): void {
        requestAnimationFrame(() => applyFit());
    }

    function applyFit(): void {
        // Never let an environment quirk (an SVG engine without getBBox or zoom
        // support) break an otherwise-good tree — auto-fit is only a nicety.
        try {
            const bounds = viewport.node()!.getBBox();
            if (!bounds.width || !bounds.height) return;
            const size = viewportSize();
            const scale = Math.min(
                1,
                0.9 / Math.max(bounds.width / size.width, bounds.height / size.height),
            );
            const tx = size.width / 2 - scale * (bounds.x + bounds.width / 2);
            const ty = size.height / 2 - scale * (bounds.y + bounds.height / 2);
            svg.call(zoomBehavior.transform, zoomIdentity.translate(tx, ty).scale(scale));
        } catch {
            /* leave the tree at its default position */
        }
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

    function viewportSize(): { width: number; height: number } {
        const parent = svg.node()?.parentElement;
        return {
            width: parent?.clientWidth || 800,
            height: parent?.clientHeight || 600,
        };
    }
}
