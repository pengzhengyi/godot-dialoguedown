import js from "@eslint/js";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
    {
        ignores: ["dist/**", "node_modules/**", "playwright-report/**", "test-results/**"],
    },
    js.configs.recommended,
    ...tseslint.configs.recommended,
    {
        files: ["src/**/*.ts", "e2e/**/*.ts", "e2e-live/**/*.ts", "*.config.ts"],
        languageOptions: {
            globals: {
                ...globals.browser,
                ...globals.node,
            },
        },
    },
    {
        // Node scripts (the live e2e webServer launcher).
        files: ["e2e-live/**/*.mjs"],
        languageOptions: {
            globals: { ...globals.node },
        },
    },
);
