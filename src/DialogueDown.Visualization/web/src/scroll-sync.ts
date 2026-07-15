/**
 * Keep the Source tab's editor and its rendered preview scrolled together, VS Code-style.
 *
 * The mapping is heading-anchored: the top of each heading in the editor is paired with
 * the top of the matching heading in the preview, and scrolling interpolates linearly
 * between those anchors (and proportionally in the gaps before the first and after the
 * last). Scenes therefore line up exactly and drift cannot accumulate across them; with
 * no headings it degrades to a straight proportional map.
 */

import type { EditorView } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";

/** Lezer-Markdown names both ATX (`## x`) and Setext (underlined) headings by level. */
const HEADING_NODE = /^(?:ATXHeading|SetextHeading)[1-6]$/;

/**
 * How long the pane that started a scroll keeps ownership after its last scroll event.
 * It only needs to outlast the echo scroll our own write triggers on the other pane (a
 * frame or two); a short window keeps switching which pane you drive feeling immediate.
 */
const DRIVER_HOLD_MS = 100;

/**
 * Map a scroll offset from one scrollable axis to another through paired anchor offsets.
 *
 * `fromAnchors[i]` (a pixel offset on the driving axis) corresponds to `toAnchors[i]`
 * (a pixel offset on the following axis) — here, the top of the i-th heading in each
 * pane. The result is a piecewise-linear interpolation through the breakpoints `0 → 0`,
 * each `fromAnchors[i] → toAnchors[i]`, and `fromMax → toMax`. Anchors that are not
 * strictly increasing on both axes (or that fall outside `(0, max)`) are dropped, so a
 * stray or duplicated heading cannot invert the map; extra anchors on either side are
 * ignored by pairing on the shorter list.
 */
export function mapScroll(
    from: number,
    fromAnchors: readonly number[],
    toAnchors: readonly number[],
    fromMax: number,
    toMax: number,
): number {
    if (fromMax <= 0 || toMax <= 0) return 0;
    const clamped = Math.max(0, Math.min(fromMax, from));

    // Build a monotonic breakpoint ladder, starting from the shared content top.
    const breaks: Array<{ from: number; to: number }> = [{ from: 0, to: 0 }];
    const pairs = Math.min(fromAnchors.length, toAnchors.length);
    for (let i = 0; i < pairs; i++) {
        const f = fromAnchors[i];
        const t = toAnchors[i];
        const last = breaks[breaks.length - 1];
        if (f > last.from && f < fromMax && t > last.to && t < toMax) {
            breaks.push({ from: f, to: t });
        }
    }
    breaks.push({ from: fromMax, to: toMax });

    for (let i = 0; i < breaks.length - 1; i++) {
        const lo = breaks[i];
        const hi = breaks[i + 1];
        if (clamped <= hi.from) {
            const span = hi.from - lo.from;
            const fraction = span <= 0 ? 0 : (clamped - lo.from) / span;
            return lo.to + fraction * (hi.to - lo.to);
        }
    }
    return toMax;
}

/** Content-relative pixel tops of every heading in the editor, top to bottom. */
function editorHeadingTops(view: EditorView): number[] {
    const tops: number[] = [];
    syntaxTree(view.state).iterate({
        enter: (node) => {
            if (HEADING_NODE.test(node.name)) {
                tops.push(view.lineBlockAt(node.from).top);
            }
        },
    });
    return tops;
}

/** Content-relative pixel tops of every heading in the preview, top to bottom. */
function previewHeadingTops(preview: HTMLElement): number[] {
    const base = preview.getBoundingClientRect().top - preview.scrollTop;
    return [...preview.querySelectorAll("h1, h2, h3, h4, h5, h6")].map(
        (heading) => heading.getBoundingClientRect().top - base,
    );
}

/**
 * Bind the editor and its preview so scrolling either one scrolls the other to the
 * matching heading (see {@link mapScroll}). Whichever pane the user scrolls owns the sync
 * for a short window ({@link DRIVER_HOLD_MS}), so the scroll our own write echoes back on
 * the other pane cannot start a feedback loop. Writes are coalesced to one per frame.
 * Returns a disposer that detaches the listeners.
 */
export function initScrollSync(view: EditorView, preview: HTMLElement): () => void {
    const editor = view.scrollDOM;
    let owner: "editor" | "preview" | null = null;
    let releaseTimer = 0;
    let frame = 0;

    const maxScroll = (element: { scrollHeight: number; clientHeight: number }): number =>
        element.scrollHeight - element.clientHeight;

    const follow = (from: HTMLElement, fromTops: number[], to: HTMLElement, toTops: number[]) => {
        to.scrollTo({
            top: mapScroll(from.scrollTop, fromTops, toTops, maxScroll(from), maxScroll(to)),
            // "instant" (not "auto") so the follow never inherits the preview's CSS
            // `scroll-behavior: smooth`; a smooth animation would fire scroll events past the
            // ownership window and let the follower drive back — a feedback loop.
            behavior: "instant",
        });
    };

    const onScroll = (who: "editor" | "preview") => () => {
        if (owner && owner !== who) return; // the other pane owns the sync right now
        owner = who;
        clearTimeout(releaseTimer);
        releaseTimer = window.setTimeout(() => (owner = null), DRIVER_HOLD_MS);
        if (frame) return; // one write per frame is enough
        frame = requestAnimationFrame(() => {
            frame = 0;
            const eTops = editorHeadingTops(view);
            const pTops = previewHeadingTops(preview);
            if (who === "editor") follow(editor, eTops, preview, pTops);
            else follow(preview, pTops, editor, eTops);
        });
    };

    const onEditorScroll = onScroll("editor");
    const onPreviewScroll = onScroll("preview");
    editor.addEventListener("scroll", onEditorScroll, { passive: true });
    preview.addEventListener("scroll", onPreviewScroll, { passive: true });

    return () => {
        editor.removeEventListener("scroll", onEditorScroll);
        preview.removeEventListener("scroll", onPreviewScroll);
        clearTimeout(releaseTimer);
        if (frame) cancelAnimationFrame(frame);
    };
}
