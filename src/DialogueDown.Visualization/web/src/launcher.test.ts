import { describe, it, expect, vi } from "vitest";
import { initLauncher, leafName, dirName, isUnder, type BrowseListing } from "./launcher";

const rootListing: BrowseListing = {
    path: "/root",
    parent: "/",
    directories: ["/root/proj"],
    sources: ["/root/a.dialogue.md"],
};
const projListing: BrowseListing = {
    path: "/root/proj",
    parent: "/root",
    directories: [],
    sources: ["/root/proj/scene.dialogue.md"],
};

function ports(overrides: Partial<ReturnType<typeof basePorts>> = {}) {
    return { ...basePorts(), ...overrides };
}

function basePorts() {
    return {
        browse: vi.fn(async (path: string) => (path === "/root/proj" ? projListing : rootListing)),
        open: vi.fn(async () => "http://127.0.0.1:1/r/proj/"),
        navigate: vi.fn<(url: string) => void>(),
    };
}

const flush = () => new Promise((resolve) => setTimeout(resolve));
const items = (root: HTMLElement, selector = ".launcher-item") =>
    [...root.querySelectorAll(selector)].map((node) => node.textContent);

describe("path helpers", () => {
    it("leafName returns the last segment (either separator)", () => {
        expect(leafName("/root/proj/scene.dialogue.md")).toBe("scene.dialogue.md");
        expect(leafName("C:\\a\\b")).toBe("b");
    });

    it("dirName returns the parent, and / at the top", () => {
        expect(dirName("/root/proj/scene.dialogue.md")).toBe("/root/proj");
        expect(dirName("/root")).toBe("/");
    });

    it("isUnder tests containment", () => {
        expect(isUnder("/root", "/root/a.dialogue.md")).toBe(true);
        expect(isUnder("/root", "/root")).toBe(true);
        expect(isUnder("/root", "/other/x")).toBe(false);
    });
});

describe("initLauncher", () => {
    it("renders the folders and sources of the start root", async () => {
        const container = document.createElement("div");
        const p = ports();

        initLauncher(container, { root: "/root", source: null, mode: "static" }, p);
        await flush();

        expect(items(container, ".launcher-item.dir")).toEqual(["proj"]);
        expect(items(container, ".launcher-item.src")).toEqual(["a.dialogue.md"]);
        expect(container.querySelector(".launcher-root-path")?.textContent).toBe("/root");
        expect(p.browse).toHaveBeenCalledWith("/root");
    });

    it("browses into a directory when clicked", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/root", source: null, mode: "static" }, p);
        await flush();

        (container.querySelector(".launcher-item.dir") as HTMLElement).click();
        await flush();

        expect(p.browse).toHaveBeenLastCalledWith("/root/proj");
        expect(items(container, ".launcher-item.src")).toEqual(["scene.dialogue.md"]);
    });

    it("selects a source and drives Open with the root", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/root", source: null, mode: "watch" }, p);
        await flush();
        const open = container.querySelector(".launcher-open") as HTMLButtonElement;

        expect(open.disabled).toBe(true);
        (container.querySelector(".launcher-item.src") as HTMLElement).click();
        expect(open.disabled).toBe(false);

        open.click();
        await flush();
        expect(p.open).toHaveBeenCalledWith("/root", "/root/a.dialogue.md", "watch");
        expect(p.navigate).toHaveBeenCalledWith("http://127.0.0.1:1/r/proj/");
    });

    it("pre-selects the initial source and browses its folder", async () => {
        const container = document.createElement("div");
        const p = ports({ browse: vi.fn(async () => projListing) });

        initLauncher(
            container,
            { root: "/root", source: "/root/proj/scene.dialogue.md", mode: "static" },
            p,
        );
        await flush();

        expect(p.browse).toHaveBeenCalledWith("/root/proj");
        expect(container.querySelector(".launcher-item.selected")?.textContent).toBe(
            "scene.dialogue.md",
        );
    });

    it("'Use current folder' sets the serving root to the browsed folder", async () => {
        const container = document.createElement("div");
        const p = ports();
        initLauncher(container, { root: "/root", source: null, mode: "static" }, p);
        await flush();
        (container.querySelector(".launcher-item.dir") as HTMLElement).click();
        await flush();

        (container.querySelector(".launcher-set-root") as HTMLElement).click();

        expect(container.querySelector(".launcher-root-path")?.textContent).toBe("/root/proj");
    });

    it("selecting a source outside the root re-roots to its folder", async () => {
        const container = document.createElement("div");
        const outside: BrowseListing = {
            path: "/other",
            parent: "/",
            directories: [],
            sources: ["/other/x.dialogue.md"],
        };
        const p = ports({ browse: vi.fn(async () => outside) });
        initLauncher(container, { root: "/root", source: null, mode: "static" }, p);
        await flush();

        (container.querySelector(".launcher-item.src") as HTMLElement).click();
        (container.querySelector(".launcher-open") as HTMLButtonElement).click();
        await flush();

        expect(p.open).toHaveBeenCalledWith("/other", "/other/x.dialogue.md", "static");
    });

    it("disables the Live Edit mode and defaults to static", async () => {
        const container = document.createElement("div");
        initLauncher(container, { root: "/root", source: null, mode: "static" }, ports());
        await flush();

        expect((container.querySelector('input[value="live"]') as HTMLInputElement).disabled).toBe(
            true,
        );
        expect((container.querySelector('input[value="static"]') as HTMLInputElement).checked).toBe(
            true,
        );
    });
});
