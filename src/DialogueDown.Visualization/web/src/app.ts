import type { Report, Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import { createSourceView } from "./source-view";
import { initResizer } from "./resizer";
import { initTooltips, initTabTooltips } from "./tooltips";
import { setHelp } from "./help";

// The Source tab shows the compiler input, not a projected stage, so its hover
// tip is a constant here rather than a field on the model.
const SOURCE_TIP = "The document as written, beside a live Markdown preview.";

/** Controls a running report: swap in fresh data, or show/clear a status banner. */
export interface AppController {
    /** Re-render the report with new data, keeping the active tab where possible. */
    rerender(report: Report): void;
    /** Show a status message (e.g. a live compile error), or clear it with `null`. */
    showBanner(message: string | null): void;
}

/**
 * Build the tabs — an optional Source tab followed by one per stage — wire the
 * shared interactions, and return a controller for live updates.
 */
export function runApp(report: Report): AppController {
    const tabsEl = document.getElementById("tabs")!;
    const stagesEl = document.getElementById("stages")!;
    const appEl = document.getElementById("app")!;
    const bannerEl = document.getElementById("live-banner")!;
    const panel = createDetailPanel();
    // Per tab: its tree view (graph tabs) or null (the Source tab, which has no
    // node-detail panel and no keyboard tree navigation).
    let views: (TreeView | null)[] = [];
    let activeIndex = 0;

    build(report);

    // One-time wiring that outlives a re-render (the containers persist).
    document.addEventListener("keydown", (event) => {
        const target = event.target as Element | null;
        if (target?.closest?.("button, input, textarea, select")) return;
        views[activeIndex]?.handleKey(event);
    });
    initResizer();
    initTooltips(stagesEl);
    initTabTooltips(tabsEl);

    return {
        rerender(next) {
            const previous = activeIndex;
            build(next);
            if (views.length > 0) activate(Math.min(previous, views.length - 1));
        },
        showBanner(message) {
            bannerEl.textContent = message ?? "";
            bannerEl.hidden = message === null;
        },
    };

    function build(report: Report): void {
        tabsEl.replaceChildren();
        stagesEl.replaceChildren();
        views = [];

        if (report.source != null) {
            const section = document.createElement("section");
            section.className = "stage source-stage";
            section.appendChild(createSourceView(report.source));
            addTab("Source", section, null, SOURCE_TIP);
        }
        for (const stage of report.stages) {
            addStageTab(stage);
        }
        if (views.length > 0) activate(0);
    }

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
        addTab(stage.title, section, view, stage.description);
    }

    function addTab(title: string, section: HTMLElement, view: TreeView | null, tip: string): void {
        const index = views.length;
        const tab = document.createElement("button");
        tab.className = "tab";
        tab.type = "button";
        tab.textContent = title;
        tab.setAttribute("data-tip", tip);
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
        const isSource = views[index] === null;
        appEl.classList.toggle("no-detail", isSource);
        setHelp(isSource ? "source" : "graph");
        // Re-fit now that the section is visible: a tree built while its tab was
        // hidden had a zero-size container, so its first fit was a no-op.
        views[index]?.fit();
        for (const view of views) view?.clearSelection();
        panel.clear();
    }
}
