import { describe, it, expect, vi } from "vitest";
import { initLauncher, leafName, parentPath, type BrowseListing } from "./launcher";

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
        navigate: vi.fn<(url: string) => void>(),
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve));
const items = (root: HTMLElement, selector = ".launcher-item") =>
    [...root.querySelectorAll(selector)].map((node) => node.textContent);

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

        initLauncher(container, { root: "/proj", source: null, mode: "static" }, p);
        await flush();

        expect(container.querySelector(".launcher-root-path")?.textContent).toBe("/proj");
        expect(items(container)).toEqual(["proj", "a.dialogue.md"]);
        expect(p.browse).toHaveBeenCalledWith("");
    });

    it("browses into a directory when it is clicked", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "static" }, p);
        await flush();

        (container.querySelector(".launcher-item.dir") as HTMLElement).click();
        await flush();

        expect(p.browse).toHaveBeenLastCalledWith("proj");
        expect(items(container, ".launcher-item.src")).toEqual(["scene.dialogue.md"]);
    });

    it("selects a source, enabling and driving Open", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/", source: null, mode: "watch" }, p);
        await flush();
        const open = container.querySelector(".launcher-open") as HTMLButtonElement;

        expect(open.disabled).toBe(true);
        (container.querySelector(".launcher-item.src") as HTMLElement).click();
        expect(open.disabled).toBe(false);

        open.click();
        await flush();
        expect(p.open).toHaveBeenCalledWith("a.dialogue.md", "watch");
        expect(p.navigate).toHaveBeenCalledWith("http://127.0.0.1:1/r/proj/");
    });

    it("pre-selects the initial source and enables Open", async () => {
        const container = document.createElement("div");
        const p = ports({ browse: vi.fn(async () => projListing) });

        initLauncher(container, { root: "/", source: "proj/scene.dialogue.md", mode: "static" }, p);
        await flush();

        expect(p.browse).toHaveBeenCalledWith("proj");
        expect(container.querySelector(".launcher-item.selected")?.textContent).toBe(
            "scene.dialogue.md",
        );
        expect((container.querySelector(".launcher-open") as HTMLButtonElement).disabled).toBe(
            false,
        );
    });

    it("disables the Live Edit mode and defaults to static", async () => {
        const container = document.createElement("div");
        initLauncher(container, { root: "/", source: null, mode: "static" }, ports());
        await flush();

        expect((container.querySelector('input[value="live"]') as HTMLInputElement).disabled).toBe(
            true,
        );
        expect((container.querySelector('input[value="static"]') as HTMLInputElement).checked).toBe(
            true,
        );
    });

    it("shows a parent row that browses up", async () => {
        const container = document.createElement("div");
        const p = ports({ browse: vi.fn(async () => projListing) });
        initLauncher(container, { root: "/", source: null, mode: "static" }, p);
        await flush();

        const up = container.querySelector(".launcher-item.up") as HTMLElement;
        expect(up.textContent).toBe("..");
        up.click();
        await flush();
        expect(p.browse).toHaveBeenLastCalledWith("");
    });
});
