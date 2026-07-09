import { describe, it, expect, vi } from "vitest";
import { startLiveClient } from "./live-client";
import type { AppController } from "./app";

/** A stand-in for the browser's EventSource (jsdom has none). */
class FakeEventSource {
    private readonly handlers: Record<string, (event: MessageEvent) => void> = {};
    addEventListener(type: string, handler: (event: MessageEvent) => void): void {
        this.handlers[type] = handler;
    }
    emit(type: string, data: string): void {
        this.handlers[type]?.(new MessageEvent(type, { data }));
    }
}

function setup() {
    const controller: AppController = { rerender: vi.fn(), showBanner: vi.fn() };
    const source = new FakeEventSource();
    const returned = startLiveClient(controller, () => source as unknown as EventSource);
    return { controller, source, returned };
}

describe("startLiveClient", () => {
    it("re-renders with the pushed report and clears the banner on a reload event", () => {
        const { controller, source } = setup();
        const report = { path: "s.dialogue.md", source: "# Hi", stages: [] };

        source.emit("reload", JSON.stringify(report));

        expect(controller.showBanner).toHaveBeenCalledWith(null);
        expect(controller.rerender).toHaveBeenCalledWith(report);
    });

    it("shows the banner and does not re-render on a problem event", () => {
        const { controller, source } = setup();

        source.emit("problem", JSON.stringify({ message: "compile error at line 3" }));

        expect(controller.showBanner).toHaveBeenCalledWith("compile error at line 3");
        expect(controller.rerender).not.toHaveBeenCalled();
    });

    it("returns the event source it connected to", () => {
        const { source, returned } = setup();
        expect(returned).toBe(source);
    });

    it("opens a real EventSource at /api/events by default", () => {
        const created: string[] = [];
        const RealEventSource = globalThis.EventSource;
        // @ts-expect-error - install a minimal fake constructor for the default path
        globalThis.EventSource = class {
            constructor(url: string) {
                created.push(url);
            }
            addEventListener(): void {}
        };
        try {
            startLiveClient({ rerender: vi.fn(), showBanner: vi.fn() });
            expect(created).toEqual(["/api/events"]);
        } finally {
            globalThis.EventSource = RealEventSource;
        }
    });
});
