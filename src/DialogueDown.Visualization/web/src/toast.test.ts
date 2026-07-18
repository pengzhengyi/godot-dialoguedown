import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { showToast } from "./toast";

describe("showToast", () => {
    beforeEach(() => {
        document.body.replaceChildren();
        vi.useFakeTimers();
    });
    afterEach(() => vi.useRealTimers());

    it("shows the message in a single, reused toast element", () => {
        showToast("Copied @N");
        showToast("Copied #hero");

        const toasts = document.querySelectorAll(".toast");
        expect(toasts).toHaveLength(1);
        expect(toasts[0].textContent).toBe("Copied #hero");
        expect(toasts[0].classList.contains("visible")).toBe(true);
        expect(toasts[0].getAttribute("role")).toBe("status");
    });

    it("hides itself after the duration elapses", () => {
        showToast("Copied @N", { durationMs: 1000 });
        const toast = document.querySelector(".toast")!;
        expect(toast.classList.contains("visible")).toBe(true);

        vi.advanceTimersByTime(1000);
        expect(toast.classList.contains("visible")).toBe(false);
    });
});
