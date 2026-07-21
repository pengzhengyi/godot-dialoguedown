import { describe, it, expect, vi } from "vitest";
import { resolveDocumentForNavigation } from "./navigation";
import type { LiveEditController, SaveResolution, SaveStatus } from "./live-edit";
import type { SaveMode } from "./save-mode";

/** A scriptable controller stub whose dirty/status/idle behavior the test drives. */
function fakeController(overrides: Partial<LiveEditController> = {}): LiveEditController {
    const base: Partial<LiveEditController> = {
        onEdit: vi.fn(),
        save: vi.fn(async () => "saved" as SaveResolution),
        flush: vi.fn(async () => "saved" as SaveResolution),
        onDiskChange: vi.fn(),
        reload: vi.fn(async () => "saved" as SaveResolution),
        adoptDisk: vi.fn(),
        whenIdle: vi.fn(async () => {}),
        discard: vi.fn(),
        discardChanges: vi.fn(),
        setMode: vi.fn(),
        mode: "auto" as SaveMode,
        dirty: false,
        status: "saved" as SaveStatus,
        canDiscard: true,
    };
    return { ...base, ...overrides } as LiveEditController;
}

describe("resolveDocumentForNavigation", () => {
    it("proceeds immediately when nothing is dirty", async () => {
        const live = fakeController({ dirty: false });

        await expect(resolveDocumentForNavigation(live, () => false)).resolves.toBe(true);
        expect(live.flush).not.toHaveBeenCalled();
    });

    it("stays in place for a paused conflict without prompting", async () => {
        const confirm = vi.fn(() => true);
        const live = fakeController({ dirty: true, status: "conflict" });

        await expect(resolveDocumentForNavigation(live, confirm)).resolves.toBe(false);
        expect(confirm).not.toHaveBeenCalled();
        expect(live.flush).not.toHaveBeenCalled();
    });

    it("Auto flushes the latest generation, looping until the buffer is clean", async () => {
        // Dirty stays true after the first flush (an edit landed during it), clean after the second.
        const live = fakeController({ mode: "auto", dirty: true, status: "saved" });
        let flushes = 0;
        (live as { flush: LiveEditController["flush"] }).flush = vi.fn(async () => {
            flushes += 1;
            if (flushes >= 2) (live as { dirty: boolean }).dirty = false;
            return "saved" as SaveResolution;
        });

        await expect(resolveDocumentForNavigation(live, () => false)).resolves.toBe(true);
        expect(live.flush).toHaveBeenCalledTimes(2);
    });

    it("Auto stays in place when a flush ends paused (conflict, waiting, error)", async () => {
        const live = fakeController({ mode: "auto", dirty: true, status: "saved" });
        (live as { flush: LiveEditController["flush"] }).flush = vi.fn(async () => {
            (live as { status: SaveStatus }).status = "conflict";
            return "conflict" as SaveResolution;
        });

        await expect(resolveDocumentForNavigation(live, () => false)).resolves.toBe(false);
        expect(live.flush).toHaveBeenCalledOnce();
    });

    it("Manual awaits the in-flight save before prompting, then discards on confirm", async () => {
        const order: string[] = [];
        const confirm = vi.fn(() => {
            order.push("confirm");
            return true;
        });
        const live = fakeController({
            mode: "manual",
            dirty: true,
            status: "dirty",
            whenIdle: vi.fn(async () => {
                order.push("whenIdle");
            }),
            discardChanges: vi.fn(() => order.push("discard")),
        });

        await expect(resolveDocumentForNavigation(live, confirm)).resolves.toBe(true);
        expect(order).toEqual(["whenIdle", "confirm", "discard"]);
    });

    it("Manual does not prompt when the awaited save settled the buffer clean", async () => {
        const confirm = vi.fn(() => false);
        // Dirty before the save settles, clean once whenIdle resolves.
        const live = fakeController({ mode: "manual", dirty: true, status: "saved" });
        (live as { whenIdle: LiveEditController["whenIdle"] }).whenIdle = vi.fn(async () => {
            (live as { dirty: boolean }).dirty = false;
        });

        await expect(resolveDocumentForNavigation(live, confirm)).resolves.toBe(true);
        expect(confirm).not.toHaveBeenCalled();
    });

    it("Manual stays in place when the reader declines the prompt", async () => {
        const live = fakeController({ mode: "manual", dirty: true, status: "dirty" });

        await expect(resolveDocumentForNavigation(live, () => false)).resolves.toBe(false);
        expect(live.discardChanges).not.toHaveBeenCalled();
    });
});
