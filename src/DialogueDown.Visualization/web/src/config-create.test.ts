import { describe, it, expect, vi, beforeEach } from "vitest";
import { createConfig, consumeOpenConfigTab, type ConfigCreatePorts } from "./config-create";

/** A minimal stub of the parts of `Response` the flow reads. */
function response(ok: boolean, body: unknown = {}): Response {
    return { ok, json: () => Promise.resolve(body) } as unknown as Response;
}

describe("createConfig", () => {
    beforeEach(() => window.sessionStorage.clear());

    it("reloads and flags the Config tab on success", async () => {
        const reload = vi.fn();
        const outcome = await createConfig({
            post: () => Promise.resolve(response(true, { outcome: "saved" })),
            reload,
        });

        expect(outcome).toBe("created");
        expect(reload).toHaveBeenCalledOnce();
        expect(consumeOpenConfigTab()).toBe(true);
    });

    it("adopts and opens a different pre-existing config as recovery", async () => {
        // The server adopted an existing dialogue.toml (200, outcome `adopted`) rather than failing.
        // The flow still reloads and opens the Config tab, so the recovery is usable.
        const reload = vi.fn();
        const outcome = await createConfig({
            post: () => Promise.resolve(response(true, { outcome: "adopted" })),
            reload,
        });

        expect(outcome).toBe("adopted");
        expect(reload).toHaveBeenCalledOnce();
        expect(consumeOpenConfigTab()).toBe(true);
    });

    it("treats an adopted saved-invalid config as a usable adoption", async () => {
        const reload = vi.fn();
        const outcome = await createConfig({
            post: () => Promise.resolve(response(true, { outcome: "adopted-invalid" })),
            reload,
        });

        expect(outcome).toBe("adopted");
        expect(reload).toHaveBeenCalledOnce();
        expect(consumeOpenConfigTab()).toBe(true);
    });

    it("throws the server's message and does not reload on conflict", async () => {
        const reload = vi.fn();
        const ports: ConfigCreatePorts = {
            post: () =>
                Promise.resolve(response(false, { message: "A dialogue.toml already exists." })),
            reload,
        };

        await expect(createConfig(ports)).rejects.toThrow("already exists");
        expect(reload).not.toHaveBeenCalled();
        expect(consumeOpenConfigTab()).toBe(false);
    });

    it("falls back to a generic message when the error has no JSON body", async () => {
        const ports: ConfigCreatePorts = {
            post: () =>
                Promise.resolve({
                    ok: false,
                    json: () => Promise.reject(new Error("no body")),
                } as unknown as Response),
            reload: vi.fn(),
        };

        await expect(createConfig(ports)).rejects.toThrow("could not be created");
    });
});

describe("consumeOpenConfigTab", () => {
    beforeEach(() => window.sessionStorage.clear());

    it("is one-shot: true once after a create, then false", async () => {
        await createConfig({ post: () => Promise.resolve(response(true)), reload: vi.fn() });

        expect(consumeOpenConfigTab()).toBe(true);
        expect(consumeOpenConfigTab()).toBe(false);
    });

    it("is false when nothing was created", () => {
        expect(consumeOpenConfigTab()).toBe(false);
    });
});
