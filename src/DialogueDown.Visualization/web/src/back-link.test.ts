import { describe, it, expect } from "vitest";
import { initBackToLauncher } from "./back-link";

describe("initBackToLauncher", () => {
    it("adds a Back-to-launcher link when served under /r/", () => {
        const header = document.createElement("header");
        header.append(document.createElement("hgroup"));

        initBackToLauncher(header, "/r/proj/");

        const link = header.querySelector<HTMLAnchorElement>("a.back-to-launcher");
        expect(link).not.toBeNull();
        expect(link!.getAttribute("href")).toBe("/");
        expect(header.firstElementChild).toBe(link);
    });

    it("adds nothing for a directly served report", () => {
        const header = document.createElement("header");

        initBackToLauncher(header, "/proj/");

        expect(header.querySelector("a.back-to-launcher")).toBeNull();
    });
});
