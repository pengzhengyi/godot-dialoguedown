import { describe, it, expect } from "vitest";
import { createSaveModeStore, defaultSaveMode, type CookieJar } from "./save-mode";

/**
 * A fake {@link CookieJar} with browser-like semantics: `write` overwrites just the one
 * `name=value` pair it names (attributes such as `Max-Age` are not stored), and `read`
 * serializes every stored pair back as `name=value; name=value`. It also records the raw
 * assignment strings so a test can assert the attributes a real browser would honor.
 */
function cookieJar(seed: Record<string, string> = {}) {
    const map = new Map<string, string>(Object.entries(seed));
    const writes: string[] = [];
    const jar: CookieJar = {
        read: () => Array.from(map, ([key, value]) => `${key}=${value}`).join("; "),
        write: (cookie) => {
            writes.push(cookie);
            const [pair] = cookie.split(";");
            const eq = pair.indexOf("=");
            map.set(pair.slice(0, eq).trim(), pair.slice(eq + 1).trim());
        },
    };
    return { map, writes, jar };
}

describe("createSaveModeStore", () => {
    it("defaults Source to Auto and Config to Manual", () => {
        const { jar } = cookieJar();

        expect(createSaveModeStore("source", jar).get()).toBe("auto");
        expect(createSaveModeStore("config", jar).get()).toBe("manual");
    });

    it("reads a persisted mode from its cookie over the default", () => {
        const { jar } = cookieJar({ "dd-save-mode-source": "manual" });

        expect(createSaveModeStore("source", jar).get()).toBe("manual");
    });

    it("parses the target cookie out of an unrelated cookie soup", () => {
        const { jar } = cookieJar({
            theme: "dark",
            "dd-save-mode-config": "auto",
            session: "abc123",
        });

        expect(createSaveModeStore("config", jar).get()).toBe("auto");
    });

    it("persists a mode under a per-document-type cookie name", () => {
        const { map, jar } = cookieJar();

        createSaveModeStore("config", jar).set("auto");

        expect(map.get("dd-save-mode-config")).toBe("auto");
        expect(createSaveModeStore("config", jar).get()).toBe("auto");
    });

    it("writes a host-scoped, long-lived, strict cookie", () => {
        const { writes, jar } = cookieJar();

        createSaveModeStore("source", jar).set("manual");

        expect(writes).toHaveLength(1);
        const cookie = writes[0];
        expect(cookie).toMatch(/^dd-save-mode-source=manual/);
        expect(cookie).toContain("SameSite=Strict");
        expect(cookie).toContain("Path=/");
        expect(cookie).toMatch(/Max-Age=\d+/);
        // No Domain attribute keeps the cookie host-only; no Secure keeps it usable on the
        // http loopback server the report is served from.
        expect(cookie).not.toContain("Domain=");
        expect(cookie).not.toContain("Secure");
        const maxAge = Number(/Max-Age=(\d+)/.exec(cookie)?.[1]);
        expect(maxAge).toBeGreaterThan(60 * 60 * 24 * 30); // at least a month
    });

    it("keeps Source and Config preferences independent", () => {
        const { jar } = cookieJar();
        createSaveModeStore("source", jar).set("manual");
        createSaveModeStore("config", jar).set("auto");

        expect(createSaveModeStore("source", jar).get()).toBe("manual");
        expect(createSaveModeStore("config", jar).get()).toBe("auto");
    });

    it("falls back to the default for an unrecognized stored value", () => {
        const { jar } = cookieJar({ "dd-save-mode-source": "sometimes" });

        expect(createSaveModeStore("source", jar).get()).toBe("auto");
    });

    it("falls back to the default when cookies are unavailable", () => {
        expect(createSaveModeStore("source", null).get()).toBe("auto");
        expect(createSaveModeStore("config", null).get()).toBe("manual");
    });

    it("swallows cookie read and write failures", () => {
        const throwing: CookieJar = {
            read: () => {
                throw new Error("blocked");
            },
            write: () => {
                throw new Error("blocked");
            },
        };
        const store = createSaveModeStore("source", throwing);

        expect(store.get()).toBe("auto");
        expect(() => store.set("manual")).not.toThrow();
    });
});

describe("defaultSaveMode", () => {
    it("reports each document type's default", () => {
        expect(defaultSaveMode("source")).toBe("auto");
        expect(defaultSaveMode("config")).toBe("manual");
    });
});
