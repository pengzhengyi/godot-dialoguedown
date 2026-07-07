import type { DisplayNode, Stage } from "./model";
import { CATEGORY_COLORS } from "./palette";
import { baseLabel } from "./text";

export interface CategoryStat {
    names: string[];
    count: number;
}

/** Per-category node counts and the distinct type names present (for the legend). */
export function categoryStats(nodes: DisplayNode[]): Record<string, CategoryStat> {
    const stats: Record<string, CategoryStat> = {};
    for (const node of nodes) {
        if (!node.category) continue;
        const stat = (stats[node.category] ??= { names: [], count: 0 });
        stat.count += 1;
        const name = baseLabel(node.label);
        if (!stat.names.includes(name)) stat.names.push(name);
    }
    return stats;
}

export interface LegendHandlers {
    onToggle(category: string, dimmed: boolean): void;
    onHover(category: string): void;
    onLeave(): void;
}

/**
 * Build the interactive legend for a stage: one row per category present, showing
 * its colour, the stage's own type name(s), and a node count. Clicking a row
 * toggles it (dimming); hovering highlights it.
 */
export function createLegend(stage: Stage, handlers: LegendHandlers): HTMLElement {
    const stats = categoryStats(stage.nodes);
    const dimmed = new Set<string>();

    const legend = document.createElement("div");
    legend.className = "legend";
    for (const category of Object.keys(CATEGORY_COLORS)) {
        const stat = stats[category];
        if (stat) {
            legend.appendChild(legendItem(category, stat.names.join(" / "), stat.count));
        }
    }
    return legend;

    function legendItem(category: string, typeName: string, count: number): HTMLButtonElement {
        const item = document.createElement("button");
        item.type = "button";
        item.className = "legend-item";
        item.setAttribute("aria-pressed", "true");

        const swatch = document.createElement("span");
        swatch.className = "swatch";
        swatch.style.background = CATEGORY_COLORS[category];

        const label = document.createElement("span");
        label.className = "legend-label";
        label.textContent = typeName;

        const countEl = document.createElement("span");
        countEl.className = "count";
        countEl.textContent = String(count);

        item.append(swatch, label, countEl);

        item.addEventListener("click", () => {
            const nowDimmed = !dimmed.has(category);
            if (nowDimmed) dimmed.add(category);
            else dimmed.delete(category);
            item.classList.toggle("muted", nowDimmed);
            item.setAttribute("aria-pressed", String(!nowDimmed));
            handlers.onToggle(category, nowDimmed);
        });
        item.addEventListener("mouseenter", () => handlers.onHover(category));
        item.addEventListener("focus", () => handlers.onHover(category));
        item.addEventListener("mouseleave", handlers.onLeave);
        item.addEventListener("blur", handlers.onLeave);
        return item;
    }
}
