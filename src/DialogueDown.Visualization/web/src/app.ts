import type { Report, Stage } from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import type { CameraTransform } from "./graph-camera";
import { GraphCameraStore } from "./graph-camera";
import { createSourceView, type SourceViewHandle } from "./source-view";
import type { DialogueSymbolSource } from "./dialogue-symbols";
import { createSemanticView } from "./semantic-view";
import { initResizer } from "./resizer";
import { initFullscreen } from "./fullscreen";
import { initCollapsiblePanel } from "./collapse-toggle";
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
     * Called when the active tab changes, with whether the Source tab is now active. Used to
     * keep the mode toggle in step with the active tab.
     */
    onActiveTabChange?(isSource: boolean): void;
    /**
     * Where the Source editor's autocompletion draws its symbols. Defaults to a document
     * scan; a served session supplies the semantic analyzer's resolved symbols merged
     * with the scan.
     */
    symbols?: DialogueSymbolSource;
    /**
     * Guards navigation (switching tabs, or selecting another node) while the session has
     * unsaved edits: returns false to block it so the reader saves or discards first.
     */
    confirmNavigation?(): boolean;
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
    let activeIndex = 0;
    let sourcePresent = false;
    let sourceHandle: SourceViewHandle | null = null;
    // The current View/Edit state, so the inspector knows whether a node is editable.
    let editable = source?.editable ?? false;

    // The inspector edits a node by splicing its new text into the current document and
    // pushing that whole document back through the Source editor (so one buffer and one Save
    // stay authoritative). Only wired for a served session.
    const panel = createDetailPanel(
        source
            ? {
                  edit: {
                      isEditable: () => editable,
                      getDocument: () => sourceHandle?.getContent() ?? "",
                      onNodeEdit: (nextDocument) => sourceHandle?.setContent(nextDocument),
                      ...(source.symbols ? { symbols: source.symbols } : {}),
                  },
              }
            : {},
    );
    // Per tab: its tree view (graph tabs) or null (the Source tab, which has no
    // node-detail panel and no keyboard tree navigation).
    let views: (TreeView | null)[] = [];
    // Per tab: its camera-store key — the stage title for a graph tab, or null for
    // the Source tab (which has no graph and no camera).
    let keys: (string | null)[] = [];
    // Remembers each stage's zoom/pan and fold across tab switches and rebuilds.
    const cameras = new GraphCameraStore();

    // The whole-window maximize mode (graphs and the source split), toggled from each
    // tab's maximize button or the `f` / Escape keys. Wired once for the app's lifetime.
    const fullscreen = initFullscreen();

    build(report);

    // One-time wiring that outlives a re-render (the containers persist).
    document.addEventListener("keydown", (event) => {
        const target = event.target as Element | null;
        if (target?.closest?.("button, input, textarea, select")) return;
        views[activeIndex]?.handleKey(event);
    });
    initResizer();
    // The node-details inspector can be hidden to give the graph the full width. Its
    // toggle lives on the resize divider, doubling as the always-present re-open handle
    // once the panel is gone; the choice is remembered across reloads.
    const resizerEl = document.getElementById("resizer");
    if (resizerEl) {
        const inspector = initCollapsiblePanel({
            container: appEl,
            collapsedClass: "detail-collapsed",
            storageKey: "dd-inspector-collapsed",
            name: "inspector",
        });
        resizerEl.appendChild(inspector.button);
    }
    initTooltips(stagesEl);
    initTabTooltips(tabsEl);

    return {
        updateStages,
        setEditable: (next) => {
            editable = next;
            sourceHandle?.setEditable(next);
            panel.setEditable(next);
        },
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
            sourceHandle = createSourceView(report.source, {
                onToggleFullscreen: fullscreen.toggle,
                ...(source ? { editable: source.editable, onChange: source.onChange } : {}),
                ...(source?.symbols ? { symbols: source.symbols } : {}),
            });
            section.appendChild(sourceHandle.element);
            addTab("Source", section, null, SOURCE_TIP, null);
        }
        for (const stage of report.stages) {
            addStageTab(stage);
        }
        if (views.length > 0) activate(0);
    }

    // Replace only the graph tabs (on a Live Edit save), leaving the Source tab and its
    // editor — and the reader's cursor — untouched. Each graph's remembered camera and
    // fold are recorded live (as the reader adjusts them), so a rebuilt stage restores
    // its position from the store.
    function updateStages(stages: Stage[]): void {
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
        // The Semantic tab has a different shape: a scene-tree graph beside stacked tables.
        // It is still a graph stage, so it reuses the tree view (camera memory, fold, full
        // screen) — only the surrounding layout differs.
        const isSemantic = stage.tables != null;
        if (isSemantic) section.classList.add("semantic-stage");
        let view: TreeView | null = null;
        try {
            const treeOptions = {
                initialCamera: cameras.cameraFor(stage.title),
                initialFold: cameras.foldFor(stage.title),
                onCameraChange: (transform: CameraTransform, byUser: boolean) =>
                    byUser
                        ? cameras.adjustCamera(stage.title, transform)
                        : cameras.noteCamera(transform),
                onFoldChange: (collapsed: string[]) => cameras.setFold(stage.title, collapsed),
                onRevert: () => cameras.reset(stage.title),
                onToggleFullscreen: fullscreen.toggle,
                // Selecting another node is navigation: block it while there are unsaved edits.
                ...(source?.confirmNavigation ? { canSelect: source.confirmNavigation } : {}),
            };
            if (isSemantic) {
                const semantic = createSemanticView(stage, panel.show, treeOptions);
                view = semantic.view;
                section.appendChild(semantic.element);
            } else {
                // A stage shows its own pinned camera, else the shared current one it
                // inherits, else the default framing; its fold is always its own. Reader
                // adjustments are recorded live through the callbacks above.
                view = createTreeView(stage, panel.show, treeOptions);
                section.appendChild(view.svg);
                section.appendChild(view.legend);
                section.appendChild(view.controls);
            }
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
        // Switching tabs is navigation: block it while there are unsaved edits so a stale
        // graph is never shown beside them (the reader saves or discards first).
        tab.addEventListener("click", () => {
            if (index !== activeIndex && source?.confirmNavigation && !source.confirmNavigation())
                return;
            activate(index);
        });
        tabsEl.appendChild(tab);
        stagesEl.appendChild(section);
        views.push(view);
        keys.push(key);
    }

    function activate(index: number): void {
        activeIndex = index;
        Array.from(tabsEl.children).forEach((el, i) => el.classList.toggle("active", i === index));
        Array.from(stagesEl.children).forEach((el, i) =>
            el.classList.toggle("active", i === index),
        );
        // The Source tab (no tree view) and the Semantic tab (its own tables) have no shared
        // node-detail inspector; hide it so their content takes the full width.
        const isSource = views[index] === null;
        const section = stagesEl.children[index] as HTMLElement | undefined;
        const isSemantic = section?.classList.contains("semantic-stage") ?? false;
        appEl.classList.toggle("no-detail", isSource || isSemantic);
        setHelp(isSource ? "source" : isSemantic ? "semantic" : "graph");
        source?.onActiveTabChange?.(isSource);
        // Frame the tab now that it is visible (a tree built while hidden had a
        // zero-size container). Applying its remembered position — instead of always
        // re-framing — keeps a stage spatially stable as the reader moves between tabs.
        revealView(index);
        for (const view of views) view?.clearSelection();
        panel.clear();
    }

    /**
     * Show a tab's position now that it is visible: its own pinned camera, the shared
     * current camera it inherits, or the default framing — plus its remembered fold.
     */
    function revealView(index: number): void {
        const view = views[index];
        const key = keys[index];
        if (!view || !key) return;
        view.applyView(cameras.cameraFor(key), cameras.foldFor(key));
    }
}
