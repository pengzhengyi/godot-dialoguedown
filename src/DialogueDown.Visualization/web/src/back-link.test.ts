import { describe, it, expect } from "vitest";
import { initBackToLauncher } from "./back-link";

function headerWithBrand(): { header: HTMLElement; brand: HTMLElement } {
    const header = document.createElement("header");
    const hgroup = document.createElement("hgroup");
    const brand = document.createElement("h1");
    brand.className = "brand";
    hgroup.append(brand);
    header.append(hgroup);
    return { header, brand };
}

describe("initBackToLauncher", () => {
    it("adds a back link immediately left of the brand when served under /r/", () => {
        const { header, brand } = headerWithBrand();

        initBackToLauncher(header, "/r/proj/");

        const link = header.querySelector<HTMLAnchorElement>("a.back-to-launcher");
        expect(link).not.toBeNull();
        expect(link!.getAttribute("href")).toBe("/");
        expect(brand.previousElementSibling).toBe(link);
    });

    it("adds nothing for a directly served report", () => {
        const { header } = headerWithBrand();

        initBackToLauncher(header, "/proj/");

        expect(header.querySelector("a.back-to-launcher")).toBeNull();
    });
});
