// @vitest-environment node

import { describe, it, expect, vi } from "vitest";
import { createModeController, type ModeControllerPorts } from "./view-edit";
import type { LiveEditController } from "./live-edit";

function fakeLive(dirty = false): LiveEditController {
    return {
        onEdit: vi.fn(),
        save: vi.fn(async () => "saved" as const),
        flush: vi.fn(async () => "saved" as const),
        onDiskChange: vi.fn(),
        reload: vi.fn(async () => "saved" as const),
        discard: vi.fn(),
        discardChanges: vi.fn(),
        setMode: vi.fn(),
        mode: "manual",
        dirty,
        status: dirty ? "dirty" : "saved",
        canDiscard: true,
    };
}

function fakePorts(overrides: Partial<ModeControllerPorts> = {}) {
    const app = {
        updateStages: vi.fn(),
        setEditable: vi.fn(),
        setContent: vi.fn(),
        setDiagnostics: vi.fn(),
        setConfigEditable: vi.fn(),
        setConfigContent: vi.fn(),
        updateConfig: vi.fn(),
        setConfigStale: vi.fn(),
        markSourceDirty: vi.fn(),
        markConfigDirty: vi.fn(),
        isConfigTabActive: vi.fn(() => false),
        showBanner: vi.fn(),
    };
    const ports: ModeControllerPorts = {
        app,
        dialogueLive: fakeLive(),
        configLive: null,
        setEditControlsVisible: vi.fn(),
        reflect: vi.fn(),
        resolveDocument: vi.fn(async () => true),
        ...overrides,
    };
    return { ports, app };
}

/** Let the async leave-Edit flush settle. */
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

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
        expect(ports.dialogueLive.onEdit).not.toHaveBeenCalled();

        c.switchTo("edit");
        c.onEditorChange("# b");
        expect(ports.dialogueLive.onEdit).toHaveBeenCalledWith("# b");
    });

    it("re-syncs the editor and graphs on a reload in View", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        c.onReload({
            source: "# fresh",
            stages: [],
            diagnostics: [
                {
                    range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                    severity: 1,
                    code: "DLG2001",
                    message: "Boom.",
                    source: "dialoguedown",
                },
            ],
        });

        expect(app.setContent).toHaveBeenCalledWith("# fresh");
        expect(app.updateStages).toHaveBeenCalledWith([]);
        expect(app.showBanner).toHaveBeenCalledWith(null);
        expect(ports.dialogueLive.onDiskChange).not.toHaveBeenCalled();
    });

    it("enters Conflict (never reloads) on an external disk change in Edit", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("edit", ports);

        c.onReload({ source: "# fresh", stages: [] });

        expect(ports.dialogueLive.onDiskChange).toHaveBeenCalledOnce();
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

    it("stays in Edit when a document cannot be resolved (a paused conflict or declined prompt)", async () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument: vi.fn(async () => false),
        });
        const c = createModeController("edit", ports);
        app.setEditable.mockClear();

        c.switchTo("view");
        await flush();

        expect(c.mode).toBe("edit");
        expect(ports.resolveDocument).toHaveBeenCalledOnce();
        expect(app.setEditable).not.toHaveBeenCalled();
        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
    });

    it("resolves each document and switches when navigation is permitted", async () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument: vi.fn(async () => true),
        });
        const c = createModeController("edit", ports);

        c.switchTo("view");
        await flush();

        expect(c.mode).toBe("view");
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
        expect(app.setEditable).toHaveBeenLastCalledWith(false);
    });

    it("ignores a switch to the current mode", () => {
        const { ports } = fakePorts();
        const c = createModeController("view", ports);
        ports.reflect = vi.fn();

        c.switchTo("view");

        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
    });

    it("makes both the Source and config editors editable in Edit", () => {
        const { ports, app } = fakePorts({ configLive: fakeLive() });
        const c = createModeController("view", ports);

        c.switchTo("edit");

        expect(app.setEditable).toHaveBeenLastCalledWith(true);
        expect(app.setConfigEditable).toHaveBeenLastCalledWith(true);
    });

    it("routes config edits to the config state machine only in Edit", () => {
        const configLive = fakeLive();
        const { ports } = fakePorts({ configLive });
        const c = createModeController("view", ports);

        c.onConfigEditorChange("x = 1"); // View: ignored
        expect(configLive.onEdit).not.toHaveBeenCalled();

        c.switchTo("edit");
        c.onConfigEditorChange("y = 2");
        expect(configLive.onEdit).toHaveBeenCalledWith("y = 2");
    });

    it("resolves both documents before leaving Edit and discards each", async () => {
        const configLive = fakeLive(true);
        const resolveDocument = vi.fn(async () => true);
        const { ports } = fakePorts({ configLive, resolveDocument });
        const c = createModeController("edit", ports);

        c.switchTo("view");
        await flush();

        expect(resolveDocument).toHaveBeenCalledTimes(2);
        expect(configLive.discard).toHaveBeenCalledOnce();
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
        expect(c.mode).toBe("view");
    });
});
