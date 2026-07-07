// Accessibility gate for the generated report. Assembles the report exactly as
// HtmlTemplate does (template + assets + a representative sample graph), renders
// it in jsdom, and runs axe-core over the result. Fails on any violation.
//
// color-contrast is disabled: it needs a real browser's layout/paint, which jsdom
// does not provide. Run Lighthouse/axe in a real browser for contrast checks.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { JSDOM, VirtualConsole } from "jsdom";
import axe from "axe-core";

const here = path.dirname(fileURLToPath(import.meta.url));
const asset = (name) => fs.readFileSync(path.join(here, "assets", name), "utf8");

const stages = [
  {
    title: "Markdown AST",
    nodes: [
      { id: "n0", label: "Document", attributes: [], category: "document", source: "# Hi\n\nWorld" },
      {
        id: "n1",
        label: "Heading (H1)",
        attributes: [{ name: "level", value: "1" }, { name: "span", value: "[0, 4)" }],
        category: "structure",
        source: "# Hi",
      },
      {
        id: "n2",
        label: "Paragraph",
        attributes: [{ name: "span", value: "[6, 11)" }],
        category: "speech",
        source: "World",
      },
      {
        id: "n3",
        label: "Image",
        attributes: [
          { name: "source", value: "x.jpg" },
          { name: "alt", value: "An image" },
          { name: "span", value: "[0, 5)" },
        ],
        category: "media",
        source: "![An image](x.jpg)",
      },
    ],
    edges: [
      { fromId: "n0", toId: "n1", kind: "Child" },
      { fromId: "n0", toId: "n2", kind: "Child" },
      { fromId: "n2", toId: "n3", kind: "Child" },
    ],
  },
];

const html = asset("report.html")
  .split("__PICO_CSS__")
  .join(asset("pico.min.css"))
  .split("__TIPPY_CSS__")
  .join(asset("tippy.css"))
  .split("__CSS__")
  .join(asset("report.css"))
  .split("__D3__")
  .join(asset("d3.v7.min.js"))
  .split("__MARKED__")
  .join(asset("marked.min.js"))
  .split("__POPPER__")
  .join(asset("popper.min.js"))
  .split("__TIPPY_JS__")
  .join(asset("tippy.umd.min.js"))
  .split("__STAGES__")
  .join(JSON.stringify(stages))
  .split("__REPORT_JS__")
  .join(asset("report.js"));

// jsdom's CSS parser cannot parse Pico's modern CSS and logs a noisy error for
// it; swallow only that, so real problems still surface.
const virtualConsole = new VirtualConsole();
virtualConsole.on("jsdomError", (error) => {
  if (!/Could not parse CSS/.test(error.message)) {
    console.error(error.message);
  }
});

const dom = new JSDOM(html, {
  runScripts: "dangerously",
  pretendToBeVisual: true,
  virtualConsole,
});
const { window } = dom;
window.SVGElement.prototype.getBBox = () => ({ x: 0, y: 0, width: 200, height: 80 });
Object.defineProperty(window.HTMLElement.prototype, "clientWidth", { get: () => 900, configurable: true });
Object.defineProperty(window.HTMLElement.prototype, "clientHeight", { get: () => 600, configurable: true });

await new Promise((resolve) => setTimeout(resolve, 250));

window.eval(axe.source);
const results = await window.axe.run(window.document.body, {
  resultTypes: ["violations"],
  rules: { "color-contrast": { enabled: false } },
});

if (results.violations.length > 0) {
  console.error("Accessibility violations found:\n");
  for (const violation of results.violations) {
    console.error(`  [${violation.impact}] ${violation.id} — ${violation.help}`);
    console.error(`      ${violation.helpUrl}`);
    for (const node of violation.nodes.slice(0, 4)) {
      console.error(`      at: ${node.target.join(" ")}`);
    }
  }
  process.exit(1);
}

console.log(
  `Accessibility check passed: ${results.passes.length} axe rules, 0 violations ` +
    "(color-contrast excluded — needs a real browser).",
);
