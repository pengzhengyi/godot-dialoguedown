import { describe, it, expect, vi } from "vitest";
import {
    createLiveEdit,
    type DiskLoad,
    type LiveEditPorts,
    type SaveOutcome,
    type SaveRequest,
    type SaveStatus,
} from "./live-edit";
import type { DocumentType } from "./save-mode";
import type { Report } from "./model";

const REPORT: Report = {
    source: "# Saved",
    stages: [{ title: "Markdown AST", description: "", nodes: [], edges: [] }],
};

function reportFor(source: string): Report {
    return { source, stages: REPORT.stages };
}

/** A controllable save port: each call parks until the test resolves it with an outcome. */
function harness(documentType: DocumentType = "source", initial = "# Saved") {
    const saves: Array<{
        request: SaveRequest;
        resolve: (outcome: SaveOutcome) => void;
        reject: (reason?: unknown) => void;
    }> = [];
    const idle: Array<() => void> = [];
    const calls = {
        applied: [] as Report[],
        content: [] as string[],
        dirty: [] as boolean[],
        status: [] as Array<{ status: SaveStatus; message?: string }>,
        unload: [] as boolean[],
    };
    let diskLoad: DiskLoad = { kind: "loaded", source: initial, report: reportFor(initial) };

    const ports: LiveEditPorts = {
        save: (request) =>
            new Promise<SaveOutcome>((resolve, reject) => saves.push({ request, resolve, reject })),
        loadFromDisk: () => Promise.resolve(diskLoad),
        applyReport: (report) => calls.applied.push(report),
        setContent: (source) => calls.content.push(source),
        setDirty: (dirty) => calls.dirty.push(dirty),
        setStatus: (status, message) => calls.status.push({ status, message }),
        setUnloadGuard: (active) => calls.unload.push(active),
        scheduleIdle: (callback) => {
            idle.push(callback);
            return () => {
                const index = idle.indexOf(callback);
                if (index >= 0) idle.splice(index, 1);
            };
        },
    };

    const onModeChange = vi.fn();
    const make = (mode: "auto" | "manual" = "auto") =>
        createLiveEdit(ports, { documentType, mode, onModeChange }, initial);

    return {
        ports,
        calls,
        saves,
        idle,
        onModeChange,
        make,
        setDiskLoad: (load: DiskLoad) => {
            diskLoad = load;
        },
        /** Fire the pending idle callback (as the fake timer would). */
        fireIdle: () => {
            const callback = idle.shift();
            callback?.();
        },
        /** Resolve the oldest pending save with an outcome and let microtasks flush. */
        resolveSave: async (outcome: SaveOutcome) => {
            const pending = saves.shift();
            pending?.resolve(outcome);
            await Promise.resolve();
            await Promise.resolve();
        },
        rejectSave: async (reason?: unknown) => {
            const pending = saves.shift();
            pending?.reject(reason);
            await Promise.resolve();
            await Promise.resolve();
        },
    };
}

const savedOutcome = (source: string): SaveOutcome => ({
    kind: "saved",
    report: reportFor(source),
    source,
});

describe("createLiveEdit — editing and dirty", () => {
    it("marks dirty on the first edit, arms the unload guard, and reports Unsaved", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");

        expect(live.dirty).toBe(true);
        expect(h.calls.dirty.at(-1)).toBe(true);
        expect(h.calls.unload.at(-1)).toBe(true);
        expect(live.status).toBe("dirty");
    });

    it("ignores a no-op edit that matches the buffer", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Saved");

        expect(live.dirty).toBe(false);
        expect(h.calls.status).toEqual([]);
    });
});

describe("createLiveEdit — Auto debounce", () => {
    it("arms an idle save 1s after an edit and saves the latest buffer", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# One");
        live.onEdit("# Two"); // restarts the idle timer; only one callback is pending
        expect(h.idle.length).toBe(1);

        h.fireIdle();
        expect(h.saves[0]!.request.source).toBe("# Two");

        await h.resolveSave(savedOutcome("# Two"));
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
        expect(h.calls.applied.length).toBe(1);
    });

    it("does not idle-save in Manual mode", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");

        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — explicit save", () => {
    it("saves immediately, applies the report, advances the baseline, and clears dirty", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        expect(h.saves[0]!.request.source).toBe("# New");
        expect(live.status).toBe("saving");

        await h.resolveSave(savedOutcome("# New"));
        await expect(done).resolves.toBe("saved");
        expect(live.dirty).toBe(false);
        expect(h.calls.applied).toEqual([reportFor("# New")]);
    });

    it("is a no-op when nothing changed since the last save", async () => {
        const h = harness();
        const live = h.make("manual");

        await expect(live.save()).resolves.toBe("noop");
        expect(h.saves.length).toBe(0);
    });

    it("cancels a pending idle timer when saving explicitly", () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        expect(h.idle.length).toBe(1);
        void live.save();

        expect(h.idle.length).toBe(0);
        expect(h.saves.length).toBe(1);
    });
});

describe("createLiveEdit — single-flight and generation safety", () => {
    it("keeps one edit-during-save dirty, does not apply its report, and follows up in Auto", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# One");
        h.fireIdle();
        live.onEdit("# Two"); // a newer edit while the save is in flight

        await h.resolveSave(savedOutcome("# One"));
        // The save advanced the baseline but must not clear dirty for the newer buffer.
        expect(live.dirty).toBe(true);
        expect(live.status).toBe("dirty");
        expect(h.calls.applied.length).toBe(0);

        // Auto schedules one follow-up idle save for the latest buffer.
        expect(h.idle.length).toBe(1);
        h.fireIdle();
        expect(h.saves[0]!.request.source).toBe("# Two");
        expect(h.saves[0]!.request.expectedBaseline).toBe("# One");
    });

    it("never runs two saves at once — a save during an in-flight save is queued", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");
        void live.save();
        live.onEdit("# Two");
        void live.save();

        expect(h.saves.length).toBe(1); // only the first is running
        await h.resolveSave(savedOutcome("# One"));
        expect(h.saves.length).toBe(1); // the queued one runs next
        expect(h.saves[0]!.request.source).toBe("# Two");
    });

    it("shares the in-flight promise for a request with the same identity", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const first = live.save();
        const second = live.save(); // same source, generation, policies → shared

        expect(h.saves.length).toBe(1);
        await h.resolveSave(savedOutcome("# New"));
        await expect(first).resolves.toBe("saved");
        await expect(second).resolves.toBe("saved");
    });

    it("replaces an older queued request with a newer generation and supersedes the older caller", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");
        void live.save();
        live.onEdit("# Two");
        const queuedOld = live.save();
        live.onEdit("# Three");
        const queuedNew = live.save();

        await expect(queuedOld).resolves.toBe("superseded");
        await h.resolveSave(savedOutcome("# One"));
        expect(h.saves[0]!.request.source).toBe("# Three");
        await h.resolveSave(savedOutcome("# Three"));
        await expect(queuedNew).resolves.toBe("saved");
    });
});

describe("createLiveEdit — failure, conflict, uncertain", () => {
    it("a failed save stays dirty, reports failure, and clears queued automatic work", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        await h.resolveSave({ kind: "failure", message: "disk full" });

        await expect(done).resolves.toBe("failure");
        expect(live.dirty).toBe(true);
        expect(live.status).toBe("error");
    });

    it("a baseline mismatch enters Conflict and cancels queued automatic work", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed on disk" });

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0);
    });

    it("a transport exception enters Uncertain and clears the queue", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        await h.rejectSave(new Error("network"));

        await expect(done).resolves.toBe("uncertain");
        expect(live.status).toBe("uncertain");
    });

    it("editing in Conflict updates the buffer but stays paused", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });
        live.onEdit("# Newer");

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0); // no auto-retry from an edit in Conflict
    });

    it("Auto does not retry a paused Error state on idle", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "failure", message: "denied" });
        // An edit recovers to Dirty and re-arms idle; without an edit, no retry runs.
        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — reload from disk", () => {
    it("replaces the buffer with external content and adopts it as the baseline", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        h.setDiskLoad({ kind: "loaded", source: "# External", report: reportFor("# External") });
        await live.reload();

        expect(h.calls.content.at(-1)).toBe("# External");
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
    });

    it("keeps Conflict when the file was deleted on disk", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        h.setDiskLoad({ kind: "missing" });
        await expect(live.reload()).resolves.toBe("conflict");
        expect(live.status).toBe("conflict");
    });
});

describe("createLiveEdit — Config validation", () => {
    it("Config Auto enters Waiting for valid TOML on an invalid-auto outcome", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("auto");

        live.onEdit("bogus");
        h.fireIdle();
        expect(h.saves[0]!.request.validation).toBe("require-valid");
        await h.resolveSave({ kind: "invalid-auto", message: "bad TOML" });

        expect(live.status).toBe("waiting");
        expect(live.dirty).toBe(true);
    });

    it("Config explicit save uses allow-invalid and reports Saved — invalid TOML", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.onEdit("bogus");
        const done = live.save();
        expect(h.saves[0]!.request.validation).toBe("allow-invalid");
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });

        await expect(done).resolves.toBe("saved-invalid");
        expect(live.status).toBe("saved-invalid");
        expect(live.dirty).toBe(false); // persisted, so not dirty
    });

    it("explicit invalid Config that completes after a newer edit advances baseline but stays dirty", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.onEdit("bogus");
        const done = live.save();
        live.onEdit("bogus2"); // newer edit during the save
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });

        await expect(done).resolves.toBe("saved-invalid");
        expect(live.dirty).toBe(true); // the newer buffer is still unsaved
    });

    it("from Waiting, an explicit save force-writes the invalid TOML", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("auto");

        live.onEdit("bogus");
        h.fireIdle();
        await h.resolveSave({ kind: "invalid-auto", message: "bad" });
        expect(live.status).toBe("waiting");

        const done = live.save();
        expect(h.saves[0]!.request.validation).toBe("allow-invalid");
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });
        await expect(done).resolves.toBe("saved-invalid");
    });
});

describe("createLiveEdit — discard", () => {
    it("discardChanges restores the last saved baseline and clears dirty", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");
        live.discardChanges();

        expect(h.calls.content.at(-1)).toBe("# Saved");
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
    });

    it("discardChanges is unavailable (a no-op) while a save is in flight", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");
        void live.save();
        expect(live.canDiscard).toBe(false);
        live.discardChanges();

        expect(h.calls.content).toEqual([]); // nothing restored while saving
    });

    it("discardChanges is unavailable in Conflict", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        expect(live.canDiscard).toBe(false);
        live.discardChanges();
        expect(live.status).toBe("conflict");
    });

    it("discardChanges from Error restores the baseline and its saved state", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        void live.save();
        await h.resolveSave({ kind: "failure", message: "denied" });
        expect(live.status).toBe("error");

        live.discardChanges();
        expect(live.status).toBe("saved");
        expect(live.dirty).toBe(false);
    });
});

describe("createLiveEdit — save-mode changes", () => {
    it("Auto → Manual cancels a pending idle timer and persists the choice", () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        expect(h.idle.length).toBe(1);
        live.setMode("manual");

        expect(h.idle.length).toBe(0);
        expect(h.onModeChange).toHaveBeenCalledWith("manual");
        expect(live.mode).toBe("manual");
    });

    it("Manual → Auto schedules the current dirty buffer from Dirty", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        expect(h.idle.length).toBe(0);
        live.setMode("auto");

        expect(h.idle.length).toBe(1);
    });

    it("Manual → Auto does not resume a paused Conflict", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        void live.save();
        await h.resolveSave({ kind: "conflict", message: "changed" });
        live.setMode("auto");

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — navigation flush", () => {
    it("flush resolves noop when there is nothing to save", async () => {
        const h = harness();
        const live = h.make("auto");

        await expect(live.flush()).resolves.toBe("noop");
    });

    it("flush saves the latest generation and resolves saved", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        const done = live.flush();
        expect(h.saves[0]!.request.source).toBe("# New");
        await h.resolveSave(savedOutcome("# New"));

        await expect(done).resolves.toBe("saved");
    });

    it("flush resolves conflict when the write conflicts, so navigation can stay in place", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        const done = live.flush();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        await expect(done).resolves.toBe("conflict");
    });
});
