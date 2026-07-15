import { describe, it, expect } from "vitest";
import { mapScroll } from "./scroll-sync";

describe("mapScroll", () => {
    it("maps proportionally when there are no anchors", () => {
        expect(mapScroll(0, [], [], 100, 200)).toBe(0);
        expect(mapScroll(50, [], [], 100, 200)).toBe(100);
        expect(mapScroll(100, [], [], 100, 200)).toBe(200);
    });

    it("pins the content top and bottom to the target ends", () => {
        expect(mapScroll(0, [40], [100], 100, 300)).toBe(0);
        expect(mapScroll(100, [40], [100], 100, 300)).toBe(300);
    });

    it("lands exactly on a heading's paired offset", () => {
        expect(mapScroll(40, [40], [100], 100, 300)).toBe(100);
    });

    it("interpolates before and after a single anchor", () => {
        // Halfway from the top (0) to the anchor (40) -> halfway from 0 to 100.
        expect(mapScroll(20, [40], [100], 100, 300)).toBe(50);
        // Halfway from the anchor (40) to the bottom (100) -> halfway from 100 to 300.
        expect(mapScroll(70, [40], [100], 100, 300)).toBe(200);
    });

    it("interpolates between two anchors", () => {
        // Anchors: 20->50, 60->250. Midway (40) between them -> midway (150).
        expect(mapScroll(40, [20, 60], [50, 250], 100, 300)).toBe(150);
    });

    it("clamps out-of-range input", () => {
        expect(mapScroll(-30, [40], [100], 100, 300)).toBe(0);
        expect(mapScroll(999, [40], [100], 100, 300)).toBe(300);
    });

    it("drops anchors that are not strictly increasing on both axes", () => {
        // The second pair (30->40) goes backwards on the driving axis after 50->90, so it
        // is ignored; the map stays monotonic and 50 still pairs with 90.
        expect(mapScroll(50, [50, 30], [90, 40], 100, 200)).toBe(90);
    });

    it("ignores anchors at the very top or bottom edge", () => {
        // Anchors at 0 and at fromMax carry no information; fall back to proportional.
        expect(mapScroll(50, [0, 100], [0, 200], 100, 200)).toBe(100);
    });

    it("pairs on the shorter anchor list when counts differ", () => {
        // Only the first pair (40->120) is usable; the unpaired target anchor is ignored.
        expect(mapScroll(40, [40], [120, 999], 80, 240)).toBe(120);
    });

    it("returns 0 when either axis cannot scroll", () => {
        expect(mapScroll(10, [], [], 0, 200)).toBe(0);
        expect(mapScroll(10, [], [], 100, 0)).toBe(0);
    });
});
