import { describe, it, expect, vi } from "vitest";
import { createModeController, type ModeControllerPorts } from "./view-edit";
import type { LiveEditController } from "./live-edit";

function fakeLive(dirty = false): LiveEditController {
    return {
        onEdit: vi.fn(),
        save: vi.fn(async () => {}),
        onDiskChange: vi.fn(),
        discard: vi.fn(),
        discardChanges: vi.fn(),
        dirty,
    };
}

function fakePorts(overrides: Partial<ModeControllerPorts> = {}) {
    const app = {
        updateStages: vi.fn(),
        setEditable: vi.fn(),
        setContent: vi.fn(),
        showBanner: vi.fn(),
    };
    const ports: ModeControllerPorts = {
        app,
        live: fakeLive(),
        setEditControlsVisible: vi.fn(),
        reflect: vi.fn(),
        confirmDiscard: vi.fn(() => true),
        ...overrides,
    };
    return { ports, app };
}

describe("createModeController", () => {
    it("configures the editor for the initial mode", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        expect(c.mode).toBe("view");
        expect(app.setEditable).toHaveBeenCalledWith(false);
        expect(ports.setEditControlsVisible).toHaveBeenCalledWith(false);
        expect(ports.reflect).toHaveBeenCalledWith("view");
    });

    it("tracks edits only in Edit mode", () => {
        const { ports } = fakePorts();
        const c = createModeController("view", ports);

        c.onEditorChange("# a"); // View: a reload, not a user edit
        expect(ports.live.onEdit).not.toHaveBeenCalled();

        c.switchTo("edit");
        c.onEditorChange("# b");
        expect(ports.live.onEdit).toHaveBeenCalledWith("# b");
    });

    it("re-syncs the editor and graphs on a reload in View", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        c.onReload({ source: "# fresh", stages: [] });

        expect(app.setContent).toHaveBeenCalledWith("# fresh");
        expect(app.updateStages).toHaveBeenCalledWith([]);
        expect(app.showBanner).toHaveBeenCalledWith(null);
        expect(ports.live.onDiskChange).not.toHaveBeenCalled();
    });

    it("only chips (never reloads) on a disk change in Edit", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("edit", ports);

        c.onReload({ source: "# fresh", stages: [] });

        expect(ports.live.onDiskChange).toHaveBeenCalledOnce();
        expect(app.setContent).not.toHaveBeenCalled();
    });

    it("switches View → Edit, making the editor editable", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        c.switchTo("edit");

        expect(c.mode).toBe("edit");
        expect(app.setEditable).toHaveBeenLastCalledWith(true);
        expect(ports.setEditControlsVisible).toHaveBeenLastCalledWith(true);
    });

    it("prompts before leaving Edit while dirty and stays put when declined", () => {
        const { ports, app } = fakePorts({
            live: fakeLive(true),
            confirmDiscard: vi.fn(() => false),
        });
        const c = createModeController("edit", ports);
        app.setEditable.mockClear();

        c.switchTo("view");

        expect(c.mode).toBe("edit");
        expect(ports.confirmDiscard).toHaveBeenCalledOnce();
        expect(app.setEditable).not.toHaveBeenCalled();
        expect(ports.live.discard).not.toHaveBeenCalled();
    });

    it("discards dirty edits and switches when the prompt is accepted", () => {
        const { ports, app } = fakePorts({
            live: fakeLive(true),
            confirmDiscard: vi.fn(() => true),
        });
        const c = createModeController("edit", ports);

        c.switchTo("view");

        expect(c.mode).toBe("view");
        expect(ports.live.discard).toHaveBeenCalledOnce();
        expect(app.setEditable).toHaveBeenLastCalledWith(false);
    });

    it("ignores a switch to the current mode", () => {
        const { ports } = fakePorts();
        const c = createModeController("view", ports);
        ports.reflect = vi.fn();

        c.switchTo("view");

        expect(ports.live.discard).not.toHaveBeenCalled();
    });
});
