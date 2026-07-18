import { describe, it, expect, afterEach } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { openSearchPanel, getSearchQuery, searchPanelOpen } from "@codemirror/search";
import { compactSearch } from "./search-panel";

let view: EditorView | null = null;

/** Mount an editor over `doc` with the compact search extension and open its panel. */
function mount(doc: string): EditorView {
    view = new EditorView({
        state: EditorState.create({ doc, extensions: [compactSearch()] }),
        parent: document.body,
    });
    openSearchPanel(view);
    return view;
}

/** The find field's input (the one wrapped in `.dd-search-field`, not the replace input). */
function findInput(v: EditorView): HTMLInputElement {
    return v.dom.querySelector<HTMLInputElement>(".dd-search-field .dd-search-input")!;
}

function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event("input"));
}

afterEach(() => {
    view?.destroy();
    view = null;
    document.body.replaceChildren();
});

describe("compactSearch panel", () => {
    it("renders one compact row with a find field, three toggles, a count, and prev/next", () => {
        const v = mount("alpha beta alpha");
        const panel = v.dom.querySelector(".dd-search")!;

        expect(panel).not.toBeNull();
        expect(panel.querySelector(".dd-search-field .dd-search-input")).not.toBeNull();
        expect(panel.querySelectorAll(".dd-search-toggle")).toHaveLength(3);
        expect(panel.querySelector(".dd-search-count")).not.toBeNull();
        expect(panel.querySelectorAll(".dd-search-nav")).toHaveLength(2);
        expect(panel.querySelector(".dd-search-close")).not.toBeNull();
    });

    it("typing publishes the query and shows the match count", () => {
        const v = mount("alpha beta alpha");

        type(findInput(v), "alpha");

        expect(getSearchQuery(v.state).search).toBe("alpha");
        expect(v.dom.querySelector(".dd-search-count")!.textContent).toContain("2");
    });

    it("the case toggle flips caseSensitive and reflects its pressed state", () => {
        const v = mount("Alpha alpha");
        type(findInput(v), "alpha");
        const caseToggle = v.dom.querySelector<HTMLButtonElement>(".dd-search-toggle")!;

        expect(getSearchQuery(v.state).caseSensitive).toBe(false);
        caseToggle.click();

        expect(getSearchQuery(v.state).caseSensitive).toBe(true);
        expect(caseToggle.getAttribute("aria-pressed")).toBe("true");
    });

    it("the next button advances the selection to a match", () => {
        const v = mount("alpha beta alpha");
        type(findInput(v), "alpha");

        const navs = v.dom.querySelectorAll<HTMLButtonElement>(".dd-search-nav");
        navs[1].click(); // [0] previous, [1] next

        const { from, to } = v.state.selection.main;
        expect(v.state.doc.sliceString(from, to)).toBe("alpha");
    });

    it("expands the replace row and Replace All rewrites every match", () => {
        const v = mount("cat cat cat");
        type(findInput(v), "cat");
        const replaceInput = v.dom.querySelector<HTMLInputElement>(".dd-search-replace-input")!;

        expect(replaceInput.hidden).toBe(true);
        v.dom.querySelector<HTMLButtonElement>(".dd-search-expand")!.click();
        expect(replaceInput.hidden).toBe(false);

        type(replaceInput, "dog");
        const actions = v.dom.querySelectorAll<HTMLButtonElement>(".dd-search-action");
        actions[actions.length - 1].click(); // "All"

        expect(v.state.doc.toString()).toBe("dog dog dog");
    });

    it("the close button dismisses the panel", () => {
        const v = mount("alpha");

        expect(searchPanelOpen(v.state)).toBe(true);
        v.dom.querySelector<HTMLButtonElement>(".dd-search-close")!.click();

        expect(searchPanelOpen(v.state)).toBe(false);
    });

    it("shows No results for a query that matches nothing", () => {
        const v = mount("alpha");

        type(findInput(v), "zzz");

        expect(v.dom.querySelector(".dd-search-count")!.textContent).toBe("No results");
    });
});
