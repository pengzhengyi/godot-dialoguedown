import type { Report, Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import { createSourceView } from "./source-view";
import { initResizer } from "./resizer";
import { initTooltips } from "./tooltips";

/**
 * Build the tabs — an optional Source tab followed by one per stage — and wire
 * the shared interactions.
 */
export function runApp(report: Report): void {
    const tabsEl = document.getElementById("tabs")!;
    const stagesEl = document.getElementById("stages")!;
    const appEl = document.getElementById("app")!;
    const panel = createDetailPanel();
    // Per tab: its tree view (graph tabs) or null (the Source tab, which has no
    // node-detail panel and no keyboard tree navigation).
    const views: (TreeView | null)[] = [];
    let activeIndex = 0;

    if (report.source != null) {
        const section = document.createElement("section");
        section.className = "stage source-stage";
        section.appendChild(createSourceView(report.source));
        addTab("Source", section, null);
    }
    for (const stage of report.stages) {
        addStageTab(stage);
    }

    document.addEventListener("keydown", (event) => {
        const target = event.target as Element | null;
        if (target?.closest?.("button, input, textarea, select")) return;
        views[activeIndex]?.handleKey(event);
    });

    initResizer();
    initTooltips(stagesEl);
    if (views.length > 0) activate(0);

    function addStageTab(stage: Stage): void {
        const section = document.createElement("section");
        section.className = "stage";
        let view: TreeView | null = null;
        try {
            view = createTreeView(stage, panel.show);
            section.appendChild(view.svg);
            section.appendChild(view.legend);
            section.appendChild(view.controls);
        } catch (error) {
            section.classList.add("error");
            section.textContent = `Failed to render stage: ${(error as Error).message}`;
        }
        addTab(stage.title, section, view);
    }

    function addTab(title: string, section: HTMLElement, view: TreeView | null): void {
        const index = views.length;
        const tab = document.createElement("button");
        tab.className = "tab";
        tab.type = "button";
        tab.textContent = title;
        tab.addEventListener("click", () => activate(index));
        tabsEl.appendChild(tab);
        stagesEl.appendChild(section);
        views.push(view);
    }

    function activate(index: number): void {
        activeIndex = index;
        Array.from(tabsEl.children).forEach((el, i) => el.classList.toggle("active", i === index));
        Array.from(stagesEl.children).forEach((el, i) =>
            el.classList.toggle("active", i === index),
        );
        // The Source tab (no tree view) has no node-detail panel; hide it so the
        // split source/preview takes the full width.
        appEl.classList.toggle("no-detail", views[index] === null);
        // Re-fit now that the section is visible: a tree built while its tab was
        // hidden had a zero-size container, so its first fit was a no-op.
        views[index]?.fit();
        for (const view of views) view?.clearSelection();
        panel.clear();
    }
}
