import { describe, it, expect, vi } from "vitest";
import {
    initLauncher,
    leafName,
    parentPath,
    type BrowseListing,
    type CreateOutcome,
} from "./launcher";

const rootListing: BrowseListing = {
    path: "",
    parent: null,
    directories: ["proj"],
    sources: ["a.dialogue.md"],
};
const projListing: BrowseListing = {
    path: "proj",
    parent: "",
    directories: [],
    sources: ["proj/scene.dialogue.md"],
};

function ports(overrides: Partial<ReturnType<typeof basePorts>> = {}) {
    return { ...basePorts(), ...overrides };
}

function basePorts() {
    return {
        browse: vi.fn(async (path: string) => (path === "proj" ? projListing : rootListing)),
        open: vi.fn(async () => "http://127.0.0.1:1/r/proj/"),
        create: vi.fn(
            async () => ({ kind: "opened", url: "http://127.0.0.1:1/r/new/" }) as CreateOutcome,
        ),
        confirm: vi.fn(() => true),
        navigate: vi.fn<(url: string) => void>(),
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve));
const items = (root: HTMLElement, selector = ".launcher-item") =>
    [...root.querySelectorAll(selector)].map((node) => node.textContent);

/** Opens the inline "New file" row, types a name, and submits it. */
async function createNamed(container: HTMLElement, name: string): Promise<void> {
    (container.querySelector(".launcher-new") as HTMLElement).click();
    (container.querySelector(".launcher-create-name") as HTMLInputElement).value = name;
    (container.querySelector(".launcher-create-submit") as HTMLElement).click();
    await flush();
}

describe("leafName / parentPath", () => {
    it("leafName returns the last path segment", () => {
        expect(leafName("proj/scene.dialogue.md")).toBe("scene.dialogue.md");
        expect(leafName("")).toBe("");
    });

    it("parentPath drops the last segment", () => {
        expect(parentPath("a/b/c")).toBe("a/b");
        expect(parentPath("a")).toBe("");
    });
});

describe("initLauncher", () => {
    it("renders the root, its folders and its sources", async () => {
        const container = document.createElement("div");
        const p = ports();

        initLauncher(container, { root: "/proj", source: null, mode: "view" }, p);
        await flush();

        expect(container.querySelector(".launcher-root-path")?.textContent).toBe("/proj");
        expect(items(container)).toEqual(["proj", "a.dialogue.md", "New file"]);
        expect(p.browse).toHaveBeenCalledWith("");
    });

    it("browses into a directory when it is clicked", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        (container.querySelector(".launcher-item.dir") as HTMLElement).click();
        await flush();

        expect(p.browse).toHaveBeenLastCalledWith("proj");
        expect(items(container, ".launcher-item.src")).toEqual(["scene.dialogue.md"]);
    });

    it("selects a source, enabling and driving Open", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();
        const open = container.querySelector(".launcher-open") as HTMLButtonElement;

        expect(open.disabled).toBe(true);
        (container.querySelector(".launcher-item.src") as HTMLElement).click();
        expect(open.disabled).toBe(false);

        open.click();
        await flush();
        expect(p.open).toHaveBeenCalledWith("a.dialogue.md", "view");
        expect(p.navigate).toHaveBeenCalledWith("http://127.0.0.1:1/r/proj/");
    });

    it("pre-selects the initial source and enables Open", async () => {
        const container = document.createElement("div");
        const p = ports({ browse: vi.fn(async () => projListing) });

        initLauncher(container, { root: "/", source: "proj/scene.dialogue.md", mode: "view" }, p);
        await flush();

        expect(p.browse).toHaveBeenCalledWith("proj");
        expect(container.querySelector(".launcher-item.selected")?.textContent).toBe(
            "scene.dialogue.md",
        );
        expect((container.querySelector(".launcher-open") as HTMLButtonElement).disabled).toBe(
            false,
        );
    });

    it("offers the View and Edit modes and defaults to View", async () => {
        const container = document.createElement("div");
        initLauncher(container, { root: "/", source: null, mode: "view" }, ports());
        await flush();

        const view = container.querySelector('.mode-toggle-option[data-mode="view"]');
        const edit = container.querySelector('.mode-toggle-option[data-mode="edit"]');
        expect(view?.getAttribute("aria-pressed")).toBe("true");
        expect(edit?.getAttribute("aria-pressed")).toBe("false");
    });

    it("opens the selected source in the chosen mode, reflecting it as the accent", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();
        expect(container.dataset.servedMode).toBe("view");

        (container.querySelector(".launcher-item.src") as HTMLElement).click();
        (container.querySelector('.mode-toggle-option[data-mode="edit"]') as HTMLElement).click();
        expect(container.dataset.servedMode).toBe("edit");
        (container.querySelector(".launcher-open") as HTMLButtonElement).click();
        await flush();

        expect(p.open).toHaveBeenCalledWith("a.dialogue.md", "edit");
    });

    it("shows a parent row that browses up", async () => {
        const container = document.createElement("div");
        const p = ports({ browse: vi.fn(async () => projListing) });
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        const up = container.querySelector(".launcher-item.up") as HTMLElement;
        expect(up.textContent).toBe("..");
        up.click();
        await flush();
        expect(p.browse).toHaveBeenLastCalledWith("");
    });

    it("creates a new file in the current folder and opens it", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        await createNamed(container, "draft");

        expect(p.create).toHaveBeenCalledWith("draft.dialogue.md");
        expect(p.navigate).toHaveBeenCalledWith("http://127.0.0.1:1/r/new/");
    });

    it("creates the new file inside the browsed sub-folder", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();
        (container.querySelector(".launcher-item.dir") as HTMLElement).click();
        await flush();

        await createNamed(container, "x");

        expect(p.create).toHaveBeenCalledWith("proj/x.dialogue.md");
    });

    it("does not double the extension when it is already typed", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        await createNamed(container, "scene.dialogue.md");

        expect(p.create).toHaveBeenCalledWith("scene.dialogue.md");
    });

    it("offers to open an existing file instead of overwriting it", async () => {
        const container = document.createElement("div");
        const p = ports({
            create: vi.fn(
                async () => ({ kind: "exists", path: "dup.dialogue.md" }) as CreateOutcome,
            ),
        });
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        await createNamed(container, "dup");

        expect(p.confirm).toHaveBeenCalled();
        expect(p.open).toHaveBeenCalledWith("dup.dialogue.md", "edit");
        expect(p.navigate).toHaveBeenCalledWith("http://127.0.0.1:1/r/proj/");
    });

    it("does nothing when the open-instead prompt is declined", async () => {
        const container = document.createElement("div");
        const p = ports({
            create: vi.fn(
                async () => ({ kind: "exists", path: "dup.dialogue.md" }) as CreateOutcome,
            ),
            confirm: vi.fn(() => false),
        });
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        await createNamed(container, "dup");

        expect(p.open).not.toHaveBeenCalled();
        expect(p.navigate).not.toHaveBeenCalled();
    });

    it("shows an inline error when creation fails", async () => {
        const container = document.createElement("div");
        const p = ports({
            create: vi.fn(
                async () =>
                    ({
                        kind: "error",
                        message: "The containing folder does not exist.",
                    }) as CreateOutcome,
            ),
        });
        initLauncher(container, { root: "/", source: null, mode: "view" }, p);
        await flush();

        await createNamed(container, "x");

        expect(container.querySelector(".launcher-create-error")?.textContent).toContain(
            "does not exist",
        );
        expect(p.navigate).not.toHaveBeenCalled();
    });

    it("relabels the secondary button Clear once a name is typed", async () => {
        const container = document.createElement("div");
        initLauncher(container, { root: "/", source: null, mode: "view" }, ports());
        await flush();

        (container.querySelector(".launcher-new") as HTMLElement).click();
        const dismiss = container.querySelector(".launcher-create-cancel") as HTMLButtonElement;
        const name = container.querySelector(".launcher-create-name") as HTMLInputElement;
        expect(dismiss.textContent).toBe("Cancel");

        name.value = "draft";
        name.dispatchEvent(new Event("input"));
        expect(dismiss.textContent).toBe("Clear");
    });

    it("Clear empties the field but keeps it open, then Cancel dismisses it", async () => {
        const container = document.createElement("div");
        initLauncher(container, { root: "/", source: null, mode: "view" }, ports());
        await flush();

        (container.querySelector(".launcher-new") as HTMLElement).click();
        const name = container.querySelector(".launcher-create-name") as HTMLInputElement;
        name.value = "draft";
        name.dispatchEvent(new Event("input"));

        // First click clears the typed text; the field stays open and the button reverts.
        (container.querySelector(".launcher-create-cancel") as HTMLButtonElement).click();
        expect((container.querySelector(".launcher-create-name") as HTMLInputElement).value).toBe(
            "",
        );
        expect(container.querySelector(".launcher-create-cancel")?.textContent).toBe("Cancel");

        // Second click (empty field) dismisses the field back to the "New file" trigger and
        // does not re-open it (the click must not bubble to the restored trigger handler).
        (container.querySelector(".launcher-create-cancel") as HTMLButtonElement).click();
        expect(container.querySelector(".launcher-create-name")).toBeNull();
        expect(container.querySelector(".launcher-new")?.textContent).toBe("New file");
    });
});
