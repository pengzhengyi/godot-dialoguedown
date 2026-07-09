import type { AppController } from "./app";
import type { Report } from "./model";

/** Where the live server pushes hot-reload events. */
const EVENTS_URL = "/api/events";

/**
 * Connect a live report to its server. Subscribes to the server's event stream
 * and, on each push, re-renders the report in place (`reload`) or shows a status
 * banner (`problem` — a compile error or a missing document). The browser's
 * `EventSource` reconnects on its own if the connection drops.
 */
export function startLiveClient(
    controller: AppController,
    createSource: () => EventSource = () => new EventSource(EVENTS_URL),
): EventSource {
    const events = createSource();

    events.addEventListener("reload", (event) => {
        const report = JSON.parse((event as MessageEvent).data) as Report;
        controller.showBanner(null);
        controller.rerender(report);
    });

    events.addEventListener("problem", (event) => {
        const { message } = JSON.parse((event as MessageEvent).data) as { message: string };
        controller.showBanner(message);
    });

    return events;
}
