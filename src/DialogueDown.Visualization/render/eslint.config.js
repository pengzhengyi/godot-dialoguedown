import js from "@eslint/js";
import globals from "globals";

// Flat ESLint config for the hand-written client script. Vendored, minified
// libraries are ignored; report.js is a browser IIFE that uses the vendored
// globals (d3, marked, tippy, Popper).
export default [
  {
    ignores: [
      "node_modules/**",
      "assets/d3.v7.min.js",
      "assets/marked.min.js",
      "assets/popper.min.js",
      "assets/tippy.umd.min.js",
    ],
  },
  {
    files: ["assets/report.js"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "script",
      globals: {
        ...globals.browser,
        d3: "readonly",
        marked: "readonly",
        tippy: "readonly",
        Popper: "readonly",
      },
    },
    rules: {
      ...js.configs.recommended.rules,
    },
  },
  {
    files: ["check-a11y.mjs"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "module",
      globals: {
        ...globals.node,
      },
    },
    rules: {
      ...js.configs.recommended.rules,
    },
  },
];
