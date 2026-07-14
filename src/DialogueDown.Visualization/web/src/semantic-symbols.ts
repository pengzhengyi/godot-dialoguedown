import {
    type DialogueSymbols,
    type DialogueSymbolSource,
    type JumpTarget,
    scanDialogueSymbols,
} from "./dialogue-symbols";

/** Append `values` to `seen`, keeping first-seen order and dropping duplicates. */
function unionInto(seen: string[], values: readonly string[]): void {
    for (const value of values) {
        if (!seen.includes(value)) {
            seen.push(value);
        }
    }
}

/**
 * Merge two symbol sets, `primary` first. Duplicates are dropped (jump targets by slug,
 * the rest by value), so the authoritative resolved symbols lead each completion list and
 * the live scan only adds what the last compile has not yet seen.
 */
function mergeSymbols(primary: DialogueSymbols, secondary: DialogueSymbols): DialogueSymbols {
    const targets = new Map<string, JumpTarget>();
    for (const target of [...primary.jumpTargets, ...secondary.jumpTargets]) {
        if (!targets.has(target.slug)) {
            targets.set(target.slug, target);
        }
    }

    const speakers: string[] = [];
    unionInto(speakers, primary.speakers);
    unionInto(speakers, secondary.speakers);

    const speakerIds: string[] = [];
    unionInto(speakerIds, primary.speakerIds);
    unionInto(speakerIds, secondary.speakerIds);

    const tags: string[] = [];
    unionInto(tags, primary.tags);
    unionInto(tags, secondary.tags);

    return { jumpTargets: [...targets.values()], speakers, speakerIds, tags };
}

/**
 * A {@link DialogueSymbolSource} backed by the semantic analyzer's resolved symbols
 * (canonical speaker ids, merged tags, validated jump targets) carried in the report
 * payload, merged with a live document scan.
 *
 * The resolved symbols lead each completion list; the scan fills in names typed since the
 * last compile so completion stays live while editing. `getSemantic` is read on every call
 * so a hot-reload can refresh the resolved symbols by updating its backing holder. When no
 * semantic symbols are available (the stage did not run) it falls back to the scan alone.
 */
export function createSemanticSymbolSource(
    getSemantic: () => DialogueSymbols | undefined,
    scan: DialogueSymbolSource = scanDialogueSymbols,
): DialogueSymbolSource {
    return (doc) => {
        const scanned = scan(doc);
        const semantic = getSemantic();
        return semantic ? mergeSymbols(semantic, scanned) : scanned;
    };
}
