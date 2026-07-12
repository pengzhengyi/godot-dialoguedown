import { describe, it, expect } from "vitest";
import { scanDialogueSymbols } from "./dialogue-symbols";

describe("scanDialogueSymbols", () => {
    it("returns empty symbol lists for an empty document", () => {
        const symbols = scanDialogueSymbols("");
        expect(symbols).toEqual({ jumpTargets: [], speakers: [], speakerIds: [], tags: [] });
    });

    it("collects scene headings as jump targets with GitHub-style slugs", () => {
        const doc = `# The Market

Alice: Hello.

## The Old Mill

Bob: Hi.`;
        const symbols = scanDialogueSymbols(doc);
        expect(symbols.jumpTargets).toEqual([
            { slug: "the-market", heading: "The Market" },
            { slug: "the-old-mill", heading: "The Old Mill" },
        ]);
    });

    it("deduplicates repeated headings the way the preview's slugger does", () => {
        const doc = `# Scene

# Scene`;
        expect(scanDialogueSymbols(doc).jumpTargets.map((t) => t.slug)).toEqual([
            "scene",
            "scene-1",
        ]);
    });

    it("collects speaker names, ids, and tags from speaker prefixes", () => {
        const doc = `Guide @guide #wise: Welcome, traveler.

Alice: Which way to the market?

@guide #patient: Take the east road.`;
        const symbols = scanDialogueSymbols(doc);
        expect(symbols.speakers).toEqual(["Guide", "Alice"]);
        expect(symbols.speakerIds).toEqual(["guide"]);
        expect(symbols.tags).toEqual(["wise", "patient"]);
    });

    it("collects trailing tags on narration and choice lines", () => {
        const doc = `The trees close in overhead. #mysterious

- Ask the guide for advice first #cautious`;
        expect(scanDialogueSymbols(doc).tags).toEqual(["mysterious", "cautious"]);
    });

    it("does not treat a line-start heading hash as a tag", () => {
        const doc = `# The Market

Alice #happy: Fresh apples!`;
        const symbols = scanDialogueSymbols(doc);
        expect(symbols.tags).toEqual(["happy"]);
        expect(symbols.jumpTargets.map((t) => t.slug)).toEqual(["the-market"]);
    });

    it("skips the YAML front matter block", () => {
        const doc = `---
title: The Curious Traveler
author: DialogueDown demo
---

# The Crossroads

Alice: Hello.`;
        const symbols = scanDialogueSymbols(doc);
        // Front-matter keys are not speakers, and the block is not scanned for tags/ids.
        expect(symbols.speakers).toEqual(["Alice"]);
        expect(symbols.jumpTargets.map((t) => t.slug)).toEqual(["the-crossroads"]);
    });

    it("ignores hashes and ats inside fenced code blocks", () => {
        const doc = `Alice: Watch this.

\`\`\`
# not a heading
@notAnId #notATag
\`\`\``;
        const symbols = scanDialogueSymbols(doc);
        expect(symbols.jumpTargets).toEqual([]);
        expect(symbols.speakerIds).toEqual([]);
        expect(symbols.tags).toEqual([]);
        expect(symbols.speakers).toEqual(["Alice"]);
    });

    it("deduplicates names, ids, and tags while keeping first-seen order", () => {
        const doc = `Alice #happy: Hi.

Alice #happy: Again.

@a: Yo.

@a: Yo again.`;
        const symbols = scanDialogueSymbols(doc);
        expect(symbols.speakers).toEqual(["Alice"]);
        expect(symbols.tags).toEqual(["happy"]);
        expect(symbols.speakerIds).toEqual(["a"]);
    });

    it("captures a quoted speaker name", () => {
        const doc = `"The Narrator": Once upon a time.`;
        expect(scanDialogueSymbols(doc).speakers).toEqual(["The Narrator"]);
    });
});
