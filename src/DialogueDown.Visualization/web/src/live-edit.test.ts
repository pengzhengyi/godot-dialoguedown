import { describe, it, expect, vi } from "vitest";
import { createLiveEdit, type LiveEditPorts } from "./live-edit";
import type { Stage } from "./model";

const STAGES = [{ title: "Markdown AST", description: "", nodes: [], edges: [] }] as Stage[];

function fakePorts(overrides: Partial<LiveEditPorts> = {}) {
    const calls = {
        setDirty: [] as boolean[],
        diskChanged: [] as boolean[],
        unloadGuard: [] as boolean[],
        updated: [] as Stage[][],
        content: [] as string[],
    };
    const ports: LiveEditPorts = {
        save: vi.fn(async () => STAGES),
        updateStages: (s) => calls.updated.push(s),
        setContent: (s) => calls.content.push(s),
        setDirty: (d) => calls.setDirty.push(d),
        setDiskChanged: (d) => calls.diskChanged.push(d),
        setUnloadGuard: (a) => calls.unloadGuard.push(a),
        ...overrides,
    };
    return { ports, calls };
}

describe("createLiveEdit", () => {
    it("marks dirty on the first edit and arms the unload guard", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports);

        live.onEdit("# Edited");

        expect(live.dirty).toBe(true);
        expect(calls.setDirty).toEqual([true]);
        expect(calls.unloadGuard).toEqual([true]);
    });

    it("does not re-fire dirty on subsequent edits", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports);

        live.onEdit("# One");
        live.onEdit("# Two");

        expect(calls.setDirty).toEqual([true]); // dirty is set once
    });

    it("save writes the current buffer, applies the recompiled stages, and clears dirty", async () => {
        const save = vi.fn(async () => STAGES);
        const { ports, calls } = fakePorts({ save });
        const live = createLiveEdit(ports);

        live.onEdit("# New");
        await live.save();

        expect(save).toHaveBeenCalledWith("# New");
        expect(calls.updated).toEqual([STAGES]);
        expect(live.dirty).toBe(false);
        expect(calls.setDirty).toEqual([true, false]);
        expect(calls.unloadGuard).toEqual([true, false]);
    });

    it("save uses the latest edited buffer", async () => {
        const save = vi.fn(async () => STAGES);
        const { ports } = fakePorts({ save });
        const live = createLiveEdit(ports, "# Seed");

        live.onEdit("# One");
        live.onEdit("# Two");
        await live.save();

        expect(save).toHaveBeenCalledWith("# Two");
    });

    it("save is a no-op when there are no unsaved edits", async () => {
        const save = vi.fn(async () => STAGES);
        const { ports } = fakePorts({ save });
        const live = createLiveEdit(ports, "# Seed");

        await live.save();

        expect(save).not.toHaveBeenCalled();
    });

    it("a failed save keeps the buffer dirty and leaves the graphs alone", async () => {
        const { ports, calls } = fakePorts({ save: vi.fn(async () => null) });
        const live = createLiveEdit(ports);

        live.onEdit("# New");
        await live.save();

        expect(live.dirty).toBe(true);
        expect(calls.updated).toEqual([]);
    });

    it("a disk change shows the passive chip and never reloads or edits", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports);

        live.onDiskChange();

        expect(calls.diskChanged).toEqual([true]);
        expect(calls.updated).toEqual([]);
        expect(live.dirty).toBe(false);
    });

    it("discard clears dirty and the disk chip without saving (leaving Edit)", () => {
        const save = vi.fn(async () => STAGES);
        const { ports, calls } = fakePorts({ save });
        const live = createLiveEdit(ports);

        live.onEdit("# New");
        live.discard();

        expect(live.dirty).toBe(false);
        expect(calls.setDirty).toEqual([true, false]);
        expect(calls.diskChanged).toEqual([false]);
        expect(save).not.toHaveBeenCalled();
    });

    it("discardChanges restores the editor to the initial version and clears dirty", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports, "# Saved");

        live.onEdit("# Edited");
        live.discardChanges();

        expect(calls.content).toEqual(["# Saved"]); // editor restored to the baseline
        expect(live.dirty).toBe(false);
        expect(calls.setDirty).toEqual([true, false]);
        expect(calls.diskChanged).toEqual([false]);
    });

    it("discardChanges restores to the most recently saved version, not the original", async () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports, "# Original");

        live.onEdit("# First");
        await live.save(); // "# First" is now the on-disk baseline
        live.onEdit("# Second");
        live.discardChanges();

        expect(calls.content).toEqual(["# First"]);
        expect(live.dirty).toBe(false);
    });

    it("discardChanges is a no-op when there are no unsaved edits", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports, "# Saved");

        live.discardChanges();

        expect(calls.content).toEqual([]); // nothing to restore
        expect(calls.setDirty).toEqual([]);
    });

    it("discardChanges does not re-mark dirty from the restore's own editor change", () => {
        // The real editor fires an onEdit when its content is replaced; the restore must not
        // be mistaken for a user edit.
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports, "# Saved");
        ports.setContent = (content) => {
            calls.content.push(content);
            live.onEdit(content); // simulate the editor's change event during restore
        };

        live.onEdit("# Edited");
        live.discardChanges();

        expect(live.dirty).toBe(false);
        expect(calls.setDirty).toEqual([true, false]); // never flips back to true
    });
});
