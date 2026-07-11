import type { Report, Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import { createSourceView, type SourceViewHandle } from "./source-view";
import { initResizer } from "./resizer";
import { initTooltips, initTabTooltips } from "./tooltips";
import { setHelp } from "./help";

// The Source tab shows the compiler input, not a projected stage, so its hover
// tip is a constant here rather than a field on the model.
const SOURCE_TIP = "The document as written, beside a live Markdown preview.";

/** How the Source tab's editor is wired for a served session. */
export interface SourceOptions {
    /** Start editable (Edit) or read-only (View); toggled later via {@link AppController.setEditable}. */
    editable: boolean;
    /** Called with the new buffer on every editor change (edits, or a View-mode reload). */
    onChange(buffer: string): void;
    /**
     * Called when the active tab changes, with whether the Source tab is now active. The
     * mode toggle governs only the Source editor, so the served wiring freezes it on the
     * read-only graph tabs and thaws it on Source.
     */
    onActiveTabChange?(isSource: boolean): void;
}

/** Controls a running report: swap in fresh data, reconfigure the editor, or show a banner. */
export interface AppController {
    /** Replace only the graph tabs with recompiled stages, leaving the Source tab (editor) intact. */
    updateStages(stages: Stage[]): void;
    /** Switch the Source editor between editable (Edit) and read-only (View) in place. */
    setEditable(editable: boolean): void;
    /** Replace the Source buffer (a View-mode hot-reload), keeping the one editor instance. */
    setContent(source: string): void;
    /** Show a status message (e.g. a live compile error), or clear it with `null`. */
    showBanner(message: string | null): void;
}

/**
 * Build the tabs — an optional Source tab followed by one per stage — wire the
 * shared interactions, and return a controller for live updates.
 */
export function runApp(report: Report, source?: SourceOptions): AppController {
    const tabsEl = document.getElementById("tabs")!;
    const stagesEl = document.getElementById("stages")!;
    const appEl = document.getElementById("app")!;
    const bannerEl = document.getElementById("live-banner")!;
    const panel = createDetailPanel();
    // Per tab: its tree view (graph tabs) or null (the Source tab, which has no
    // node-detail panel and no keyboard tree navigation).
    let views: (TreeView | null)[] = [];
    let activeIndex = 0;
    let sourcePresent = false;
    let sourceHandle: SourceViewHandle | null = null;

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
        updateStages,
        setEditable: (editable) => sourceHandle?.setEditable(editable),
        setContent: (next) => sourceHandle?.setContent(next),
        showBanner(message) {
            bannerEl.textContent = message ?? "";
            bannerEl.hidden = message === null;
        },
    };

    function build(report: Report): void {
        tabsEl.replaceChildren();
        stagesEl.replaceChildren();
        views = [];
        sourcePresent = report.source != null;

        if (report.source != null) {
            const section = document.createElement("section");
            section.className = "stage source-stage";
            sourceHandle = createSourceView(
                report.source,
                source ? { editable: source.editable, onChange: source.onChange } : {},
            );
            section.appendChild(sourceHandle.element);
            addTab("Source", section, null, SOURCE_TIP);
        }
        for (const stage of report.stages) {
            addStageTab(stage);
        }
        if (views.length > 0) activate(0);
    }

    // Replace only the graph tabs (on a Live Edit save), leaving the Source tab and its
    // editor — and the reader's cursor — untouched.
    function updateStages(stages: Stage[]): void {
        const keep = sourcePresent ? 1 : 0;
        while (tabsEl.children.length > keep) tabsEl.lastElementChild!.remove();
        while (stagesEl.children.length > keep) stagesEl.lastElementChild!.remove();
        views = views.slice(0, keep);
        for (const stage of stages) {
            addStageTab(stage);
        }
        if (views.length > 0) activate(Math.min(activeIndex, views.length - 1));
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
        source?.onActiveTabChange?.(isSource);
        // Re-fit now that the section is visible: a tree built while its tab was
        // hidden had a zero-size container, so its first fit was a no-op.
        views[index]?.fit();
        for (const view of views) view?.clearSelection();
        panel.clear();
    }
}
