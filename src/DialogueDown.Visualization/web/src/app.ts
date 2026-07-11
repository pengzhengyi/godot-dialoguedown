import type { Report, Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import { GraphCameraStore } from "./graph-camera";
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
    // Per tab: its camera-store key — the stage title for a graph tab, or null for
    // the Source tab (which has no graph and no camera).
    let keys: (string | null)[] = [];
    // Remembers each stage's zoom/pan and fold across tab switches and rebuilds.
    const cameras = new GraphCameraStore();
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
        keys = [];
        sourcePresent = report.source != null;

        if (report.source != null) {
            const section = document.createElement("section");
            section.className = "stage source-stage";
            sourceHandle = createSourceView(
                report.source,
                source ? { editable: source.editable, onChange: source.onChange } : {},
            );
            section.appendChild(sourceHandle.element);
            addTab("Source", section, null, SOURCE_TIP, null);
        }
        for (const stage of report.stages) {
            addStageTab(stage);
        }
        if (views.length > 0) activate(0);
    }

    // Replace only the graph tabs (on a Live Edit save), leaving the Source tab and its
    // editor — and the reader's cursor — untouched. Each graph's camera and fold are
    // snapshotted first, then handed back to its rebuilt stage so the reload stays put.
    function updateStages(stages: Stage[]): void {
        snapshotCameras();
        const keep = sourcePresent ? 1 : 0;
        while (tabsEl.children.length > keep) tabsEl.lastElementChild!.remove();
        while (stagesEl.children.length > keep) stagesEl.lastElementChild!.remove();
        views = views.slice(0, keep);
        keys = keys.slice(0, keep);
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
            // A rebuilt stage inherits its predecessor's remembered position (if any).
            view = createTreeView(stage, panel.show, cameras.load(stage.title));
            section.appendChild(view.svg);
            section.appendChild(view.legend);
            section.appendChild(view.controls);
        } catch (error) {
            section.classList.add("error");
            section.textContent = `Failed to render stage: ${(error as Error).message}`;
        }
        addTab(stage.title, section, view, stage.description, stage.title);
    }

    function addTab(
        title: string,
        section: HTMLElement,
        view: TreeView | null,
        tip: string,
        key: string | null,
    ): void {
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
        keys.push(key);
    }

    function activate(index: number): void {
        // Remember where the reader left the tab we are leaving, before switching.
        snapshotActive();
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
        // Restore the tab's remembered position, or fit it the first time it is shown
        // (a tree built while hidden had a zero-size container, so its first fit was a
        // no-op). Restoring — instead of always re-fitting — keeps a stage spatially
        // stable as the reader moves between tabs.
        revealCamera(index);
        for (const view of views) view?.clearSelection();
        panel.clear();
    }

    /** Remember the active graph tab's camera + fold before switching away from it. */
    function snapshotActive(): void {
        const key = keys[activeIndex];
        const view = views[activeIndex];
        if (key && view) cameras.save(key, view.getState());
    }

    /** Remember every graph tab's camera + fold before a rebuild replaces the views. */
    function snapshotCameras(): void {
        views.forEach((view, i) => {
            const key = keys[i];
            if (key && view) cameras.save(key, view.getState());
        });
    }

    /** Restore a tab's remembered position, or fit it on its first reveal. */
    function revealCamera(index: number): void {
        const view = views[index];
        if (!view) return;
        const key = keys[index];
        const saved = key ? cameras.load(key) : undefined;
        if (saved) view.restore(saved);
        else view.fit();
    }
}
