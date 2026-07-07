import type { Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import { initResizer } from "./resizer";
import { initTooltips } from "./tooltips";

/** Build the tabs and stages, and wire the shared interactions. */
export function runApp(stages: Stage[]): void {
    const tabsEl = document.getElementById("tabs")!;
    const stagesEl = document.getElementById("stages")!;
    const panel = createDetailPanel();
    const views: (TreeView | null)[] = [];
    let activeIndex = 0;

    stages.forEach((stage, index) => {
        tabsEl.appendChild(createTab(stage, index));

        const section = document.createElement("section");
        section.className = "stage";
        stagesEl.appendChild(section);

        try {
            const view = createTreeView(stage, panel.show);
            section.appendChild(view.svg);
            section.appendChild(view.legend);
            section.appendChild(view.controls);
            views.push(view);
        } catch (error) {
            section.classList.add("error");
            section.textContent = `Failed to render stage: ${(error as Error).message}`;
            views.push(null);
        }
    });

    document.addEventListener("keydown", (event) => {
        const target = event.target as Element | null;
        if (target?.closest?.("button, input, textarea, select")) return;
        views[activeIndex]?.handleKey(event);
    });

    initResizer();
    initTooltips(stagesEl);
    if (stages.length > 0) activate(0);

    function createTab(stage: Stage, index: number): HTMLButtonElement {
        const tab = document.createElement("button");
        tab.className = "tab";
        tab.type = "button";
        tab.textContent = stage.title;
        tab.addEventListener("click", () => activate(index));
        return tab;
    }

    function activate(index: number): void {
        activeIndex = index;
        Array.from(tabsEl.children).forEach((el, i) => el.classList.toggle("active", i === index));
        Array.from(stagesEl.children).forEach((el, i) =>
            el.classList.toggle("active", i === index),
        );
        for (const view of views) view?.clearSelection();
        panel.clear();
    }
}
