import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { runScopeHandlers } from "@codemirror/view";
import {
    CompletionContext,
    completionStatus,
    startCompletion,
    type CompletionResult,
} from "@codemirror/autocomplete";
import { scanDialogueSymbols } from "./dialogue-symbols";
import {
    jumpTargetCompletions,
    speakerIdCompletions,
    tagCompletions,
    speakerCompletions,
    dialogueAutocompletion,
} from "./editor-completions";

/** Build a completion context with the cursor at `|` in `docWithCursor`. */
function contextAtCursor(docWithCursor: string, explicit = false): CompletionContext {
    const pos = docWithCursor.indexOf("|");
    const doc = docWithCursor.slice(0, pos) + docWithCursor.slice(pos + 1);
    return new CompletionContext(EditorState.create({ doc }), pos, explicit);
}

/** The labels a source offers at the cursor, or `null` when it does not fire. */
function labelsAt(
    source: ReturnType<typeof jumpTargetCompletions>,
    docWithCursor: string,
): string[] | null {
    const result = resultAt(source, docWithCursor);
    return result ? result.options.map((o) => o.label) : null;
}

/** The (synchronous) completion result a source returns at the cursor, or `null`. */
function resultAt(
    source: ReturnType<typeof jumpTargetCompletions>,
    docWithCursor: string,
): CompletionResult | null {
    return source(contextAtCursor(docWithCursor)) as CompletionResult | null;
}

describe("jumpTargetCompletions", () => {
    const source = jumpTargetCompletions(scanDialogueSymbols);

    it("offers heading slugs inside a jump destination", () => {
        const doc = `# The Market

## The Old Mill

Alice: Go [east](#|)`;
        expect(labelsAt(source, doc)).toEqual(["the-market", "the-old-mill"]);
    });

    it("filters against the partial slug via the completion's from", () => {
        const doc = `# The Market

Alice: Go [east](#the-m|)`;
        const result = resultAt(source, doc)!;
        // `from` points just after `#`, so the whole partial slug is the filter prefix.
        const state = EditorState.create({ doc: doc.replace("|", "") });
        expect(state.doc.sliceString(result.from, doc.indexOf("|"))).toBe("the-m");
    });

    it("shows the heading text as the option detail", () => {
        const doc = `# The Market

Alice: [x](#|)`;
        const result = resultAt(source, doc)!;
        expect(result.options[0]).toMatchObject({ label: "the-market", detail: "The Market" });
    });

    it("types each option as dd-jump (selects the arrow icon)", () => {
        const doc = `# The Market

Alice: [x](#|)`;
        const result = resultAt(source, doc)!;
        expect(result.options.every((o) => o.type === "dd-jump")).toBe(true);
    });

    it("does not fire outside a jump destination", () => {
        expect(labelsAt(source, `# The Market\n\nAlice: plain text |`)).toBeNull();
    });

    it("does not fire when the document has no headings", () => {
        expect(labelsAt(source, `Alice: Go [east](#|)`)).toBeNull();
    });
});

describe("speakerIdCompletions", () => {
    const source = speakerIdCompletions(scanDialogueSymbols);

    it("offers declared ids after an @", () => {
        const doc = `Guide @guide: Hi.

Merchant @merchant: Wares!

@|`;
        expect(labelsAt(source, doc)).toEqual(["guide", "merchant"]);
    });

    it("does not fire without an @ before the cursor", () => {
        expect(labelsAt(source, `Guide @guide: Hi.\n\nAlice: plain|`)).toBeNull();
    });

    it("excludes the half-typed id from its own suggestions", () => {
        const doc = `Guide @guide: Hi.

@gu|`;
        // The in-progress `@gu` is scanned as an id too; it must not suggest itself.
        expect(labelsAt(source, doc)).toEqual(["guide"]);
    });

    it("types each option as dd-speaker-id (selects the @ icon)", () => {
        const doc = `Guide @guide: Hi.

@|`;
        const result = resultAt(source, doc)!;
        expect(result.options.every((o) => o.type === "dd-speaker-id")).toBe(true);
    });
});

describe("tagCompletions", () => {
    const source = tagCompletions(scanDialogueSymbols);

    it("offers tags after a mid-line #", () => {
        const doc = `Guide #wise: Hi.

Alice #happy: Yo.

Bob #|`;
        expect(labelsAt(source, doc)).toEqual(["wise", "happy"]);
    });

    it("does not fire on a line-start # (a Markdown heading)", () => {
        const doc = `Guide #wise: Hi.

#|`;
        expect(labelsAt(source, doc)).toBeNull();
    });

    it("does not fire inside a jump destination", () => {
        const doc = `Guide #wise: Hi.

Alice: [x](#|)`;
        expect(labelsAt(source, doc)).toBeNull();
    });

    it("types each option as dd-tag (selects the # icon)", () => {
        const doc = `Guide #wise: Hi.

Bob #|`;
        const result = resultAt(source, doc)!;
        expect(result.options.every((o) => o.type === "dd-tag")).toBe(true);
    });
});

describe("speakerCompletions", () => {
    const source = speakerCompletions(scanDialogueSymbols);

    it("offers known speakers at the start of a line", () => {
        const doc = `Alice: Hi.

Guide @g: Hello.

A|`;
        // Returns every known speaker; CodeMirror filters by the typed prefix.
        expect(labelsAt(source, doc)).toEqual(["Alice", "Guide"]);
    });

    it("does not fire mid-line, after the speaker name", () => {
        const doc = `Alice: Hi.

Alice: some text |`;
        expect(labelsAt(source, doc)).toBeNull();
    });

    it("does not fire when the document has no speakers", () => {
        expect(labelsAt(source, `# Heading\n\nA|`)).toBeNull();
    });

    it("types each option as dd-speaker (selects the person icon)", () => {
        const doc = `Alice: Hi.

A|`;
        const result = resultAt(source, doc)!;
        expect(result.options.every((o) => o.type === "dd-speaker")).toBe(true);
    });
});

describe("dialogueAutocompletion keymap", () => {
    /** Mount an editable editor with the dialogue completions, cursor at the end of `doc`. */
    function mount(doc: string): EditorView {
        const parent = document.createElement("div");
        document.body.appendChild(parent);
        return new EditorView({
            parent,
            state: EditorState.create({
                doc,
                selection: { anchor: doc.length },
                extensions: [dialogueAutocompletion()],
            }),
        });
    }

    /**
     * Open the completion tooltip and wait until Tab/Enter would actually accept it.
     * Two waits are needed: the completion query is async (poll until `active`), and
     * CodeMirror enforces a 75 ms `interactionDelay` after a tooltip opens — an
     * anti-misclick guard that blocks accepting a *just*-opened completion (it applies
     * to Enter too). A human always clears it; the extra wait mirrors that.
     */
    async function openCompletion(view: EditorView, timeout = 1000): Promise<void> {
        startCompletion(view);
        const start = Date.now();
        while (completionStatus(view.state) !== "active") {
            if (Date.now() - start > timeout) throw new Error("completion never activated");
            await new Promise((resolve) => setTimeout(resolve, 10));
        }
        await new Promise((resolve) => setTimeout(resolve, 120)); // clear interactionDelay
    }

    /** Send Tab through the editor's keymap; returns whether a binding consumed it. */
    function pressTab(view: EditorView): boolean {
        const event = new KeyboardEvent("keydown", { key: "Tab" });
        return runScopeHandlers(view, event, "editor");
    }

    it("accepts the active completion, like Enter", async () => {
        const view = mount(`Alice: Hi.

A`);
        await openCompletion(view);
        const consumed = pressTab(view);
        expect(view.state.doc.toString()).toBe(`Alice: Hi.

Alice`);
        expect(consumed).toBe(true);
        view.destroy();
    });

    it("leaves Tab alone when no completion is open, so focus can exit the editor", () => {
        const view = mount(`Alice: Hi.

A`);
        // No startCompletion: with the tooltip closed, Tab must fall through to its
        // default (moving focus), never getting swallowed by the editor.
        const consumed = pressTab(view);
        expect(view.state.doc.toString()).toBe(`Alice: Hi.

A`);
        expect(consumed).toBe(false);
        view.destroy();
    });
});
