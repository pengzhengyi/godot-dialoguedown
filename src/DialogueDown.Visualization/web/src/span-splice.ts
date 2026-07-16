/**
 * Splice a node edit back into the document.
 *
 * A node in a graph carries a source {@link Span} — a half-open `[start, end)` character
 * range into the original document. Editing that node replaces exactly that range with the
 * new text, so an edit to one node never disturbs the rest of the document and identical
 * text elsewhere is never touched (unlike a find/replace). A zero-width span inserts at its
 * position — the groundwork for editing a synthetic node in a later iteration.
 */

/** A half-open `[start, end)` character range into a document. */
export interface Span {
    start: number;
    end: number;
}

/**
 * Return a new document with the node's `[start, end)` range replaced by `text`. Offsets are
 * clamped to the document and ordered, so a stray span can only ever produce a no-op rather
 * than corrupt the document.
 */
export function spanSplice(document: string, span: Span, text: string): string {
    const start = clamp(span.start, 0, document.length);
    const end = clamp(span.end, start, document.length);
    return document.slice(0, start) + text + document.slice(end);
}

function clamp(value: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, value));
}
