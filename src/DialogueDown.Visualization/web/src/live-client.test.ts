import { describe, it, expect, vi } from "vitest";
import { watchServerEvents } from "./live-client";

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
    const handlers = { onReload: vi.fn(), onReloadConfig: vi.fn(), onProblem: vi.fn() };
    const source = new FakeEventSource();
    const returned = watchServerEvents(handlers, () => source as unknown as EventSource);
    return { handlers, source, returned };
}

describe("watchServerEvents", () => {
    it("routes a reload event's report to onReload", () => {
        const { handlers, source } = setup();
        const report = { path: "s.dialogue.md", source: "# Hi", stages: [] };

        source.emit("reload", JSON.stringify(report));

        expect(handlers.onReload).toHaveBeenCalledWith(report);
        expect(handlers.onProblem).not.toHaveBeenCalled();
    });

    it("routes a reload-config event's report to onReloadConfig", () => {
        const { handlers, source } = setup();
        const report = { path: "s.dialogue.md", stages: [], outcome: "loaded" };

        source.emit("reload-config", JSON.stringify(report));

        expect(handlers.onReloadConfig).toHaveBeenCalledWith(report);
        expect(handlers.onReload).not.toHaveBeenCalled();
    });

    it("routes a problem event's message to onProblem", () => {
        const { handlers, source } = setup();

        source.emit("problem", JSON.stringify({ message: "compile error at line 3" }));

        expect(handlers.onProblem).toHaveBeenCalledWith("compile error at line 3", undefined);
        expect(handlers.onReload).not.toHaveBeenCalled();
    });

    it("forwards a disk problem's target so it can route to the matching controller", () => {
        const { handlers, source } = setup();

        source.emit(
            "problem",
            JSON.stringify({ message: "Configuration not found: dialogue.toml", target: "config" }),
        );

        expect(handlers.onProblem).toHaveBeenCalledWith(
            "Configuration not found: dialogue.toml",
            "config",
        );
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
            watchServerEvents({ onReload: vi.fn(), onReloadConfig: vi.fn(), onProblem: vi.fn() });
            expect(created).toEqual(["/api/events"]);
        } finally {
            globalThis.EventSource = RealEventSource;
        }
    });
});
