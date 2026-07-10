import { defineConfig, type Plugin } from "vitest/config";
import { existsSync, renameSync } from "node:fs";
import { resolve } from "node:path";
import { viteSingleFile } from "vite-plugin-singlefile";

// vite-plugin-singlefile inlines everything into ONE file and disables code
// splitting, so each self-contained page (report, launcher) is built separately —
// `VITE_ENTRY=launcher` selects the launcher build. See the `build` script.
const launcherBuild = process.env.VITE_ENTRY === "launcher";

// Rename the report build artifact to report.html — a clearer name than Vite's
// default index.html for the file the .NET library embeds. Done in a plugin (not
// a shell `mv`) so it is portable across platforms. The launcher build emits
// launcher.html directly, so there is nothing to rename.
function renameToReport(): Plugin {
    return {
        name: "rename-to-report",
        closeBundle() {
            const index = resolve("dist", "index.html");
            if (existsSync(index)) renameSync(index, resolve("dist", "report.html"));
        },
    };
}

// Each page is shipped as ONE self-contained HTML file that the .NET library embeds.
// vite-plugin-singlefile inlines all JS and CSS; the .NET side injects per-page data
// into the page's slot (report stages, or the launcher's initial selection).
export default defineConfig({
    root: ".",
    plugins: [viteSingleFile(), renameToReport()],
    build: {
        target: "es2022",
        outDir: "dist",
        // Clear dist/ on the report build (the first one); keep report.html on the
        // launcher build so both self-contained pages survive.
        emptyOutDir: !launcherBuild,
        rollupOptions: {
            input: resolve(launcherBuild ? "launcher.html" : "index.html"),
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
                "src/launcher-main.ts",
                "src/model.ts",
                "src/app.ts",
                "src/tree-view.ts",
                "src/tooltips.ts",
                "src/source-view.ts",
                "src/live-edit-ui.ts",
            ],
        },
    },
});
