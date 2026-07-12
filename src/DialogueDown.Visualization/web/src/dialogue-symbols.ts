import GithubSlugger from "github-slugger";
import { splitFrontMatter } from "./text";

/** One completable jump destination: a scene heading's anchor and its display text. */
export interface JumpTarget {
    /** The GitHub-style slug inserted after `#` — the same anchor the preview links to. */
    slug: string;
    /** The heading text, shown as the completion's detail. */
    heading: string;
}

/** The names a document contains, grouped by the DSL concept each completes. */
export interface DialogueSymbols {
    /** Scene-heading anchors, for completing a jump destination `](#…)`. */
    jumpTargets: JumpTarget[];
    /** Speaker display names, for completing a line's leading speaker. */
    speakers: string[];
    /** Speaker stable ids (without the `@`), for completing `@id`. */
    speakerIds: string[];
    /** Speaker/line tags (without the `#`), for completing `#tag`. */
    tags: string[];
}

/**
 * The seam: where the editor's completion symbols come from for a given document. The
 * default is {@link scanDialogueSymbols} (a text scan); a later source can read the
 * semantic analyzer's resolved symbols instead, without changing the editor glue.
 */
export type DialogueSymbolSource = (doc: string) => DialogueSymbols;

// A scene heading: one-to-six leading hashes, the text, then optional closing hashes.
const HEADING = /^(#{1,6})[ \t]+(.+?)[ \t]*#*[ \t]*$/;
// A line's leading speaker name (identifier or quoted), when followed by `@`, `#`, or `:`.
const SPEAKER_PREFIX = /^[ \t]*(?:"([^"]+)"|([A-Za-z][\w'’-]*))[ \t]*(?=[@#:])/;
// A fence that opens or closes a code block (``` or ~~~).
const CODE_FENCE = /^[ \t]*(`{3,}|~{3,})/;
const SPEAKER_ID = /@([A-Za-z][\w-]*)/g;
const TAG = /#([A-Za-z][\w-]*)/g;

/**
 * Read the completable names out of a dialogue document: scene-heading jump targets,
 * speaker names, `@id`s, and `#tag`s. Front matter and fenced code blocks are skipped so
 * their `#`/`@` tokens are not mistaken for symbols. This is the default
 * {@link DialogueSymbolSource}; a scan sees only what is typed (no cross-file speakers or
 * validation), which the semantic source will later improve on.
 */
export function scanDialogueSymbols(doc: string): DialogueSymbols {
    const slugger = new GithubSlugger();
    const jumpTargets: JumpTarget[] = [];
    const speakers = new OrderedSet();
    const speakerIds = new OrderedSet();
    const tags = new OrderedSet();

    let inCodeFence = false;
    for (const line of splitFrontMatter(doc).body.split("\n")) {
        if (CODE_FENCE.test(line)) {
            inCodeFence = !inCodeFence;
            continue;
        }
        if (inCodeFence) continue;

        const heading = HEADING.exec(line);
        if (heading) {
            const text = heading[2];
            jumpTargets.push({ slug: slugger.slug(text), heading: text });
            continue;
        }

        const speaker = SPEAKER_PREFIX.exec(line);
        if (speaker) speakers.add(speaker[1] ?? speaker[2]);

        for (const match of line.matchAll(SPEAKER_ID)) speakerIds.add(match[1]);

        const firstNonSpace = line.search(/\S/);
        for (const match of line.matchAll(TAG)) {
            // A line-start hash is a heading (handled above), and `](#slug)` is a jump
            // destination — neither is a tag.
            const isLineStart = match.index === firstNonSpace;
            const isLinkDestination = line[match.index - 1] === "(";
            if (!isLineStart && !isLinkDestination) tags.add(match[1]);
        }
    }

    return {
        jumpTargets,
        speakers: speakers.values,
        speakerIds: speakerIds.values,
        tags: tags.values,
    };
}

/** A tiny insertion-ordered, de-duplicating string collector. */
class OrderedSet {
    private readonly seen = new Set<string>();
    readonly values: string[] = [];

    add(value: string): void {
        if (this.seen.has(value)) return;
        this.seen.add(value);
        this.values.push(value);
    }
}
