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
    };
    const ports: LiveEditPorts = {
        save: vi.fn(async () => STAGES),
        updateStages: (s) => calls.updated.push(s),
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

        live.onEdit();

        expect(live.dirty).toBe(true);
        expect(calls.setDirty).toEqual([true]);
        expect(calls.unloadGuard).toEqual([true]);
    });

    it("does not re-fire dirty on subsequent edits", () => {
        const { ports, calls } = fakePorts();
        const live = createLiveEdit(ports);

        live.onEdit();
        live.onEdit();

        expect(calls.setDirty).toEqual([true]); // dirty is set once
    });

    it("save writes the buffer, applies the recompiled stages, and clears dirty", async () => {
        const save = vi.fn(async () => STAGES);
        const { ports, calls } = fakePorts({ save });
        const live = createLiveEdit(ports);

        live.onEdit();
        await live.onSave("# New");

        expect(save).toHaveBeenCalledWith("# New");
        expect(calls.updated).toEqual([STAGES]);
        expect(live.dirty).toBe(false);
        expect(calls.setDirty).toEqual([true, false]);
        expect(calls.unloadGuard).toEqual([true, false]);
    });

    it("a failed save keeps the buffer dirty and leaves the graphs alone", async () => {
        const { ports, calls } = fakePorts({ save: vi.fn(async () => null) });
        const live = createLiveEdit(ports);

        live.onEdit();
        await live.onSave("# New");

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
});
