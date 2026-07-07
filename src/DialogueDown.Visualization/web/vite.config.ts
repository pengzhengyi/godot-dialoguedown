import { defineConfig } from "vitest/config";
import { viteSingleFile } from "vite-plugin-singlefile";

// The report is shipped as ONE self-contained HTML file that the .NET library
// embeds. vite-plugin-singlefile inlines all JS and CSS into dist/report.html;
// the .NET side only injects the per-report stage data into the __STAGES__ slot.
export default defineConfig({
    root: ".",
    plugins: [viteSingleFile()],
    build: {
        target: "es2022",
        outDir: "dist",
        emptyOutDir: true,
        rollupOptions: {
            output: {
                // Keep a stable, predictable output filename for the .NET embed.
                entryFileNames: "report.js",
            },
        },
    },
    test: {
        environment: "jsdom",
        include: ["src/**/*.test.ts"],
        coverage: {
            provider: "v8",
            include: ["src/**/*.ts"],
            // Test files, dev-only helpers, and the browser-integration modules that
            // are exercised end-to-end by Playwright (d3 layout, tippy, wiring) rather
            // than by jsdom unit tests.
            exclude: [
                "src/**/*.test.ts",
                "src/dev-stages.ts",
                "src/main.ts",
                "src/model.ts",
                "src/app.ts",
                "src/tree-view.ts",
                "src/tooltips.ts",
            ],
        },
    },
});
