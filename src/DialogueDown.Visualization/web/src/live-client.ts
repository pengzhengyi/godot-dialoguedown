import type { Report } from "./model";

/** Where the served session pushes hot-reload and problem events. */
const EVENTS_URL = "/api/events";

/** Handlers for the served session's event stream. */
export interface ServerEventHandlers {
    /** A recompiled report was pushed (the document changed on disk). */
    onReload(report: Report): void;
    /** A recompiled report was pushed for an external configuration change. */
    onReloadConfig(report: Report): void;
    /** A compile error or a missing document — a message to surface. */
    onProblem(message: string): void;
}

/**
 * Subscribe to the served session's event stream. On each push it routes a `reload`
 * (a recompiled report from a document change), a `reload-config` (a recompiled report from an
 * external configuration change), or a `problem` (a message) to the handlers; the mode controller
 * decides what to do with a reload (View re-syncs, Edit chips). The browser's `EventSource`
 * reconnects on its own if the connection drops.
 */
export function watchServerEvents(
    handlers: ServerEventHandlers,
    createSource: () => EventSource = () => new EventSource(EVENTS_URL),
): EventSource {
    const events = createSource();

    events.addEventListener("reload", (event) => {
        handlers.onReload(JSON.parse((event as MessageEvent).data) as Report);
    });

    events.addEventListener("reload-config", (event) => {
        handlers.onReloadConfig(JSON.parse((event as MessageEvent).data) as Report);
    });

    events.addEventListener("problem", (event) => {
        const { message } = JSON.parse((event as MessageEvent).data) as { message: string };
        handlers.onProblem(message);
    });

    return events;
}
