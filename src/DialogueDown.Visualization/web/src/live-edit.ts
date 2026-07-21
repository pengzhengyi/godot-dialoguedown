import type { Report } from "./model";
import type { DocumentType, SaveMode } from "./save-mode";

/** Whether a save must produce valid Config (Auto/navigation) or may persist invalid TOML (explicit). */
export type ValidationPolicy = "require-valid" | "allow-invalid";

/** Whether a save checks the expected baseline, or force-overwrites after explicit confirmation. */
export type ConflictPolicy = "check-baseline" | "overwrite";

/** Why the client wants a save now — client-side scheduling metadata that controls urgency. */
export type SaveTrigger = "idle" | "explicit" | "navigation";

/**
 * The immutable snapshot submitted for one write: the source, its target file, the edit
 * generation it belongs to, the baseline it expects on disk, and the validation/conflict
 * policies the server must honor. The trigger stays client-side and is not sent to the server,
 * but it is captured here so queue precedence can prefer a flush over an idle save.
 */
export interface SaveRequest {
    source: string;
    target?: "config";
    generation: number;
    expectedBaseline: string;
    validation: ValidationPolicy;
    conflict: ConflictPolicy;
    trigger: SaveTrigger;
}

/**
 * The typed result of one write attempt. A transport exception (the port throwing) is not a
 * value here — it surfaces as {@link SaveStatus} `uncertain` because the client cannot know
 * whether the disk write committed.
 */
export type SaveOutcome =
    | { kind: "saved"; report: Report; source: string }
    | { kind: "saved-invalid"; report: Report; source: string; message: string }
    | { kind: "invalid-auto"; message: string }
    | { kind: "conflict"; message: string }
    | { kind: "failure"; message: string };

/** The current disk contents, fetched for a Reload from a conflict/uncertain state. */
export type DiskLoad =
    | { kind: "loaded"; source: string; report: Report }
    | { kind: "invalid"; source: string; report: Report; message: string }
    | { kind: "missing" }
    | { kind: "failure"; message: string };

/**
 * The controller's externally observable save status, mapped to an accessible status message by
 * the UI: Unsaved, Saving…, Saved, Conflict, waiting for valid TOML, Saved — invalid TOML,
 * uncertain, or failure.
 */
export type SaveStatus =
    "saved" | "dirty" | "saving" | "conflict" | "waiting" | "saved-invalid" | "uncertain" | "error";

/**
 * How one save request a caller submitted settled. A caller that shared an in-flight request or
 * won the queue learns its true outcome; a caller whose queued request was replaced by a newer
 * one settles `superseded` and must re-read the controller's current state rather than assume it
 * saved.
 */
export type SaveResolution =
    | "saved"
    | "saved-invalid"
    | "conflict"
    | "waiting"
    | "failure"
    | "uncertain"
    | "superseded"
    | "noop";

/**
 * The side-effecting collaborators the live-edit logic drives, injected so the state machine is
 * testable without a real editor, server, or DOM.
 */
export interface LiveEditPorts {
    /** Submit a save snapshot; resolves to a typed outcome, or throws to signal an uncertain transport. */
    save(request: SaveRequest): Promise<SaveOutcome>;
    /** Fetch the document's current on-disk payload for a Reload from conflict/uncertain. */
    loadFromDisk(): Promise<DiskLoad>;
    /** Apply an accepted report beyond the editor: graph tabs, config, symbols, and diagnostics. */
    applyReport(report: Report): void;
    /** Replace the editor's content — restoring a saved baseline (discard) or external text (reload). */
    setContent(source: string): void;
    /** Show or hide this document's unsaved (dirty) marker. */
    setDirty(dirty: boolean): void;
    /** Publish the accessible save status (and an optional detail message). */
    setStatus(status: SaveStatus, message?: string): void;
    /** Arm or disarm the guard that warns before leaving with unsaved edits or an in-flight save. */
    setUnloadGuard(active: boolean): void;
    /** Schedule the 1,000 ms idle callback; returns a canceler. Replaced by a fake timer in tests. */
    scheduleIdle(callback: () => void): () => void;
}

/** Drives the Source/Config document's edit → save → conflict lifecycle in Live Edit. */
export interface LiveEditController {
    /** The buffer changed — record it, advance the edit generation, and (in Auto) arm the idle timer. */
    onEdit(buffer: string): void;
    /** Explicit Save (button or ⌘/Ctrl-S) or a confirmed overwrite: request an immediate write. */
    save(options?: { overwrite?: boolean }): Promise<SaveResolution>;
    /** Flush the latest generation and await it before navigation continues (navigation urgency). */
    flush(): Promise<SaveResolution>;
    /** The file changed on disk under an active buffer — pause in Conflict; never clobber the buffer. */
    onDiskChange(report?: Report): void;
    /** Reload from disk — replace the buffer with the external content and adopt it as the baseline. */
    reload(): Promise<SaveResolution>;
    /** Adopt a clean external reload (View hot reload): set the baseline/buffer without marking dirty. */
    adoptDisk(source: string, valid?: boolean): void;
    /** Resolve once no save is in flight or queued, so a caller can act on a settled state. */
    whenIdle(): Promise<void>;
    /** Leaving Edit — drop any unsaved-dirty and paused state without saving. */
    discard(): void;
    /** Discard button — restore the editor to the last saved baseline and clear dirty. */
    discardChanges(): void;
    /** Change and (via the store) persist this document's save mode. */
    setMode(mode: SaveMode): void;
    /** The active save mode. */
    readonly mode: SaveMode;
    /** Whether the buffer differs from the saved baseline. */
    readonly dirty: boolean;
    /** The current save status. */
    readonly status: SaveStatus;
    /** Whether Discard is currently available (not while saving, in Conflict, or uncertain). */
    readonly canDiscard: boolean;
}

/** Options for constructing a controller: its document type, initial mode, and a mode-change sink. */
export interface LiveEditOptions {
    documentType: DocumentType;
    mode: SaveMode;
    /**
     * Whether the initial buffer is a valid, accepted document. Defaults to `true`; pass `false`
     * to initialize into the saved-invalid (report stale) state — used when a served page loads
     * with a persisted-but-invalid Config, so a reload restores that state.
     */
    initialValid?: boolean;
    /** Called after {@link LiveEditController.setMode} so the caller can persist the choice. */
    onModeChange?(mode: SaveMode): void;
}

interface PendingSave {
    request: SaveRequest;
    waiters: Array<(resolution: SaveResolution) => void>;
}

const PAUSED: ReadonlySet<SaveStatus> = new Set<SaveStatus>([
    "conflict",
    "error",
    "uncertain",
    "waiting",
]);

/**
 * The Live Edit state machine. It owns one document's buffer, saved baseline, edit generation,
 * dirty state, idle timer, single-flight save, and conflict/error/uncertain recovery. Every
 * confirmed write advances the saved baseline; only a response for the latest generation may
 * clear dirty, apply its report, or report Saved. Saves never overlap: a matching request shares
 * the in-flight promise, and a single queued slot always holds the strongest, newest follow-up.
 */
export function createLiveEdit(
    ports: LiveEditPorts,
    options: LiveEditOptions,
    initialBuffer = "",
): LiveEditController {
    const { documentType } = options;
    let mode: SaveMode = options.mode;
    let buffer = initialBuffer;
    let savedBaseline = initialBuffer;
    let baselineValid = options.initialValid ?? true;
    let generation = 0;
    let status: SaveStatus = baselineValid ? "saved" : "saved-invalid";
    let restoring = false;
    let inFlight: PendingSave | null = null;
    let queued: PendingSave | null = null;
    let cancelIdle: (() => void) | null = null;
    // Resolved once no save is in flight or queued; navigation awaits a settled state through it.
    let idleWaiters: Array<() => void> = [];
    // A monotonic token so only the latest reload response may replace the buffer (single-flight).
    let reloadToken = 0;

    const isDirty = (): boolean => buffer !== savedBaseline;
    const isPaused = (): boolean => PAUSED.has(status);

    function setStatus(next: SaveStatus, message?: string): void {
        status = next;
        ports.setStatus(next, message);
    }

    function refreshChrome(): void {
        ports.setDirty(isDirty());
        ports.setUnloadGuard(isDirty() || inFlight !== null);
    }

    function armIdle(): void {
        clearIdle();
        cancelIdle = ports.scheduleIdle(() => {
            cancelIdle = null;
            void requestSave("idle");
        });
    }

    function clearIdle(): void {
        if (cancelIdle !== null) {
            cancelIdle();
            cancelIdle = null;
        }
    }

    function settle(waiters: Array<(r: SaveResolution) => void>, resolution: SaveResolution): void {
        for (const waiter of waiters) waiter(resolution);
    }

    function clearQueue(): void {
        if (queued !== null) {
            settle(queued.waiters, "superseded");
            queued = null;
        }
    }

    function validationFor(trigger: SaveTrigger): ValidationPolicy {
        if (trigger === "explicit") return "allow-invalid";
        return documentType === "config" ? "require-valid" : "allow-invalid";
    }

    function buildRequest(trigger: SaveTrigger, overwrite: boolean): SaveRequest {
        return {
            source: buffer,
            ...(documentType === "config" ? { target: "config" as const } : {}),
            generation,
            expectedBaseline: savedBaseline,
            validation: validationFor(trigger),
            conflict: overwrite ? "overwrite" : "check-baseline",
            trigger,
        };
    }

    // A higher rank wins the single queued slot for the same generation: a confirmed overwrite
    // beats a baseline check, an allow-invalid write beats require-valid, and a flush beats an
    // idle save. A newer generation always wins outright (handled in enqueue).
    function rank(request: SaveRequest): number {
        const overwrite = request.conflict === "overwrite" ? 4 : 0;
        const allowInvalid = request.validation === "allow-invalid" ? 2 : 0;
        const flush = request.trigger !== "idle" ? 1 : 0;
        return overwrite + allowInvalid + flush;
    }

    function sameIdentity(a: SaveRequest, b: SaveRequest): boolean {
        return (
            a.source === b.source &&
            a.generation === b.generation &&
            a.validation === b.validation &&
            a.conflict === b.conflict
        );
    }

    function requestSave(
        trigger: SaveTrigger,
        saveOptions: { overwrite?: boolean } = {},
    ): Promise<SaveResolution> {
        const overwrite = saveOptions.overwrite ?? false;

        // Auto never retries a paused state; only an edit (Waiting) or an explicit action recovers.
        if (trigger === "idle" && (isPaused() || !isDirty())) {
            return Promise.resolve<SaveResolution>("noop");
        }
        if (trigger === "navigation" && !isDirty() && !isPaused()) {
            return Promise.resolve<SaveResolution>("noop");
        }
        if (trigger === "explicit" && !isDirty() && !isPaused()) {
            return Promise.resolve<SaveResolution>("noop");
        }

        clearIdle();
        const request = buildRequest(trigger, overwrite);

        if (inFlight !== null) {
            if (sameIdentity(inFlight.request, request)) {
                return new Promise<SaveResolution>((resolve) => inFlight!.waiters.push(resolve));
            }
            return enqueue(request);
        }
        return run(request, []);
    }

    function enqueue(request: SaveRequest): Promise<SaveResolution> {
        return new Promise<SaveResolution>((resolve) => {
            if (queued === null) {
                queued = { request, waiters: [resolve] };
                return;
            }
            const current = queued;
            if (request.generation > current.request.generation) {
                settle(current.waiters, "superseded");
                queued = { request, waiters: [resolve] };
                return;
            }
            if (request.generation < current.request.generation) {
                resolve("superseded");
                return;
            }
            if (sameIdentity(current.request, request)) {
                current.waiters.push(resolve);
                return;
            }
            if (rank(request) > rank(current.request)) {
                settle(current.waiters, "superseded");
                queued = { request, waiters: [resolve] };
                return;
            }
            resolve("superseded");
        });
    }

    function run(
        request: SaveRequest,
        waiters: Array<(r: SaveResolution) => void>,
    ): Promise<SaveResolution> {
        const pending: PendingSave = { request, waiters };
        inFlight = pending;
        setStatus("saving");
        refreshChrome();
        return Promise.resolve(ports.save(request)).then(
            (outcome) => complete(pending, outcome),
            () => complete(pending, { kind: "__throw__" }),
        );
    }

    function complete(
        pending: PendingSave,
        outcome: SaveOutcome | { kind: "__throw__" },
    ): SaveResolution {
        inFlight = null;
        const { request } = pending;
        const latest = request.generation === generation;
        let resolution: SaveResolution;

        if (outcome.kind === "__throw__") {
            resolution = enterUncertain();
        } else if (outcome.kind === "saved") {
            resolution = onSaved(request, outcome, latest);
        } else if (outcome.kind === "saved-invalid") {
            resolution = onSavedInvalid(request, outcome, latest);
        } else if (outcome.kind === "invalid-auto") {
            resolution = onInvalidAuto(outcome, latest);
        } else if (outcome.kind === "conflict") {
            resolution = onConflict(outcome);
        } else {
            resolution = onFailure(outcome);
        }

        settle(pending.waiters, resolution);
        promoteQueue();
        return resolution;
    }

    function onSaved(
        request: SaveRequest,
        outcome: Extract<SaveOutcome, { kind: "saved" }>,
        latest: boolean,
    ): SaveResolution {
        advanceBaseline(request.source, true);
        if (latest) {
            ports.applyReport(outcome.report);
            setStatus("saved");
        } else {
            setStatus("dirty");
        }
        refreshChrome();
        return "saved";
    }

    function onSavedInvalid(
        request: SaveRequest,
        outcome: Extract<SaveOutcome, { kind: "saved-invalid" }>,
        latest: boolean,
    ): SaveResolution {
        // The invalid Config was persisted, so the baseline advances and dirty clears, but the
        // last valid report stays visibly stale until the config becomes valid again.
        advanceBaseline(request.source, false);
        if (latest) {
            setStatus("saved-invalid", outcome.message);
        } else {
            setStatus("dirty");
        }
        refreshChrome();
        return "saved-invalid";
    }

    function onInvalidAuto(
        outcome: Extract<SaveOutcome, { kind: "invalid-auto" }>,
        latest: boolean,
    ): SaveResolution {
        if (!latest) {
            // A newer edit exists: discard this stale outcome; the queue or idle timer handles it.
            setStatus("dirty");
            refreshChrome();
            if (queued === null && mode === "auto" && isDirty()) armIdle();
            return "superseded";
        }
        setStatus("waiting", outcome.message);
        refreshChrome();
        return "waiting";
    }

    function onConflict(outcome: Extract<SaveOutcome, { kind: "conflict" }>): SaveResolution {
        clearIdle();
        clearQueue();
        setStatus("conflict", outcome.message);
        refreshChrome();
        return "conflict";
    }

    function onFailure(outcome: Extract<SaveOutcome, { kind: "failure" }>): SaveResolution {
        clearIdle();
        clearQueue();
        setStatus("error", outcome.message);
        refreshChrome();
        return "failure";
    }

    function enterUncertain(): SaveResolution {
        clearIdle();
        clearQueue();
        setStatus("uncertain");
        refreshChrome();
        return "uncertain";
    }

    function advanceBaseline(source: string, valid: boolean): void {
        savedBaseline = source;
        baselineValid = valid;
    }

    function promoteQueue(): void {
        if (inFlight !== null) return;
        if (queued !== null && !isPaused()) {
            const next = queued;
            queued = null;
            // Rebase onto the baseline the predecessor confirmed on disk: the request was queued
            // with the older baseline, but the file it now replaces is the predecessor's write.
            next.request = { ...next.request, expectedBaseline: savedBaseline };
            void run(next.request, next.waiters);
            return;
        }
        // A successful save that left newer edits behind schedules one idle follow-up in Auto.
        if (!isPaused() && isDirty() && mode === "auto" && cancelIdle === null) {
            armIdle();
        }
        notifyIdle();
    }

    function notifyIdle(): void {
        if (inFlight !== null || queued !== null) return;
        const waiters = idleWaiters;
        idleWaiters = [];
        for (const waiter of waiters) waiter();
    }

    function noteEdit(next: string): void {
        buffer = next;
        generation += 1;
    }

    function restoreTo(source: string, valid: boolean): void {
        clearIdle();
        clearQueue();
        restoring = true;
        ports.setContent(source);
        restoring = false;
        buffer = source;
        generation += 1;
        baselineValid = valid;
        setStatus(valid ? "saved" : "saved-invalid");
        refreshChrome();
    }

    return {
        get mode() {
            return mode;
        },
        get dirty() {
            return isDirty();
        },
        get status() {
            return status;
        },
        get canDiscard() {
            return inFlight === null && status !== "conflict" && status !== "uncertain";
        },
        onEdit(next) {
            if (restoring) return;
            if (next === buffer) return;
            noteEdit(next);
            if (status === "conflict" || status === "uncertain") {
                // Record the edit but stay paused until Reload or a confirmed overwrite.
                refreshChrome();
                return;
            }
            if (status !== "saving") setStatus("dirty");
            refreshChrome();
            if (mode === "auto") armIdle();
        },
        save(saveOptions = {}) {
            return requestSave("explicit", saveOptions);
        },
        flush() {
            return requestSave("navigation");
        },
        onDiskChange() {
            clearIdle();
            clearQueue();
            setStatus("conflict", "The file changed on disk.");
            refreshChrome();
        },
        async reload() {
            // Single-flight: a newer reload (or an edit that advances the buffer) supersedes this
            // one, so a delayed response never overwrites a newer generation.
            const token = ++reloadToken;
            const startGeneration = generation;
            const load = await ports.loadFromDisk();
            if (token !== reloadToken || generation !== startGeneration) {
                return "superseded";
            }
            if (load.kind === "missing") {
                setStatus("conflict", "The file was deleted on disk.");
                refreshChrome();
                return "conflict";
            }
            if (load.kind === "failure") {
                setStatus("conflict", load.message);
                refreshChrome();
                return "conflict";
            }
            const source = load.source;
            restoring = true;
            ports.setContent(source);
            restoring = false;
            buffer = source;
            generation += 1;
            savedBaseline = source;
            if (load.kind === "invalid") {
                baselineValid = false;
                setStatus("saved-invalid", load.message);
                refreshChrome();
                return "saved-invalid";
            }
            baselineValid = true;
            ports.applyReport(load.report);
            setStatus("saved");
            refreshChrome();
            return "saved";
        },
        adoptDisk(source, valid = true) {
            // A View hot reload already updated the editor and graphs; adopt the external content
            // as the clean baseline so a later Edit session starts from it, not stale text.
            clearIdle();
            clearQueue();
            buffer = source;
            savedBaseline = source;
            generation += 1;
            baselineValid = valid;
            setStatus(valid ? "saved" : "saved-invalid");
            refreshChrome();
        },
        whenIdle() {
            return inFlight === null && queued === null
                ? Promise.resolve()
                : new Promise<void>((resolve) => idleWaiters.push(resolve));
        },
        discard() {
            clearIdle();
            clearQueue();
            buffer = savedBaseline;
            generation += 1;
            setStatus(baselineValid ? "saved" : "saved-invalid");
            refreshChrome();
        },
        discardChanges() {
            if (inFlight !== null || status === "conflict" || status === "uncertain") return;
            if (!isDirty() && status !== "waiting" && status !== "error") return;
            restoreTo(savedBaseline, baselineValid);
        },
        setMode(next) {
            if (next !== mode) {
                mode = next;
                options.onModeChange?.(next);
            }
            if (mode === "manual") {
                clearIdle();
                // A queued explicit follow-up survives; cancel only an idle-scheduled one.
                if (queued !== null && queued.request.trigger === "idle") clearQueue();
            } else if (isDirty() && !isPaused() && inFlight === null && cancelIdle === null) {
                armIdle();
            }
        },
    };
}
