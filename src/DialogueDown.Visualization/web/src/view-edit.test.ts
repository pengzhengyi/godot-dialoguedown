// @vitest-environment node

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
        expect(app.setDiagnostics).toHaveBeenCalledWith([
            {
                range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                severity: 1,
                code: "DLG2001",
                message: "Boom.",
                source: "dialoguedown",
            },
        ]);
        expect(app.showBanner).toHaveBeenCalledWith(null);
        expect(ports.dialogueLive.onDiskChange).not.toHaveBeenCalled();
    });

    it("only chips (never reloads) on a disk change in Edit", () => {
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

    it("prompts before leaving Edit while dirty and stays put when declined", () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            confirmDiscard: vi.fn(() => false),
        });
        const c = createModeController("edit", ports);
        app.setEditable.mockClear();

        c.switchTo("view");

        expect(c.mode).toBe("edit");
        expect(ports.confirmDiscard).toHaveBeenCalledOnce();
        expect(app.setEditable).not.toHaveBeenCalled();
        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
    });

    it("discards dirty edits and switches when the prompt is accepted", () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            confirmDiscard: vi.fn(() => true),
        });
        const c = createModeController("edit", ports);

        c.switchTo("view");

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

    it("prompts before leaving Edit when only the config is dirty, and discards both", () => {
        const configLive = fakeLive(true);
        const { ports } = fakePorts({ configLive, confirmDiscard: vi.fn(() => true) });
        const c = createModeController("edit", ports);

        c.switchTo("view");

        expect(ports.confirmDiscard).toHaveBeenCalledOnce();
        expect(configLive.discard).toHaveBeenCalledOnce();
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
        expect(c.mode).toBe("view");
    });
});
