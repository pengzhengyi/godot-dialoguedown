/*
 * Client for the DialogueDown compilation-visualization report.
 *
 * window.STAGES holds one entry per compiler stage: { title, nodes, edges }.
 * This script renders each stage as an interactive D3 tree and drives the
 * detail panel, the colour legend, keyboard navigation, and the panel resizer.
 *
 * The file is organised top-down into small, focused units:
 *   - palette helpers        (category -> colour + label)
 *   - text helpers           (escaping, Markdown rendering)
 *   - categoryStats()        (per-category node counts + type names)
 *   - createDetailPanel()    (the side panel showing a selected node)
 *   - createTreeView()       (one stage's D3 tree, its legend, and interactions)
 *   - initResizer()          (drag to resize the detail panel)
 *   - initTooltips()         (Tippy.js hover tooltips over the graph nodes)
 *   - initApp()              (wires tabs, stages, keyboard, resizer together)
 */
(function () {
  "use strict";

  /* ------------------------------------------------------------------ *
   * Semantic colour palette
   *
   * The projection tags each node with a stable, cross-stage category; a later
   * stage reuses the same name for a corresponding concept, so the two share a
   * colour (a Markdown code span and the game call it compiles to are both
   * "call" = red). Only the colour is shared across stages — the human label in
   * the legend is derived from each stage's own node types, so the Markdown AST
   * legend reads "Code span", not the future "Game call".
   * ------------------------------------------------------------------ */

  var CATEGORY_COLORS = {
    document: "#64748b",
    structure: "#3b82f6",
    speech: "#22c55e",
    text: "#14b8a6",
    choice: "#a855f7",
    jump: "#06b6d4",
    media: "#f97316",
    call: "#ef4444",
    styling: "#f59e0b",
    break: "#9ca3af",
  };
  var DEFAULT_COLOR = "#94a3b8";

  // Factor each zoom-in/out button press multiplies (or divides) the scale by.
  var ZOOM_STEP = 1.3;

  /** The colour for a category, falling back to a neutral colour for unknowns. */
  function colorOf(category) {
    return (category && CATEGORY_COLORS[category]) || DEFAULT_COLOR;
  }

  /** A node's type name without any parenthetical detail ("Heading (H2)" -> "Heading"). */
  function baseLabel(label) {
    return label.replace(/\s*\(.*\)\s*$/, "");
  }

  /* ------------------------------------------------------------------ *
   * Text helpers
   * ------------------------------------------------------------------ */

  // Longest inline label/attribute drawn on a node before it is ellipsised.
  var MAX_INLINE_TEXT = 30;

  /** Escape a value for safe insertion into HTML. */
  function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, function (ch) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch];
    });
  }

  /** Shorten a string to a maximum length with an ellipsis. */
  function ellipsize(text, max) {
    return text.length > max ? text.slice(0, max - 1) + "…" : text;
  }

  /** HTML for a node's hover tooltip: its label and full (untruncated) attributes. */
  function tooltipHtml(data) {
    var parts = ["<strong>" + escapeHtml(data.label) + "</strong>"];
    (data.attributes || []).forEach(function (attr) {
      parts.push("<div>" + escapeHtml(attr.name) + ": " + escapeHtml(attr.value) + "</div>");
    });
    return parts.join("");
  }

  /** Render Markdown to HTML with marked, handling a leading YAML front matter. */
  function renderMarkdown(source) {
    var parts = splitFrontMatter(source);
    var head = parts.frontMatter
      ? "<h4>Front matter</h4><pre class=\"frontmatter\"><code>" +
        escapeHtml(parts.frontMatter) +
        "</code></pre>"
      : "";
    return head + renderMarkdownBody(parts.body);
  }

  function renderMarkdownBody(body) {
    if (window.marked && typeof window.marked.parse === "function") {
      try {
        return window.marked.parse(body);
      } catch {
        /* fall through to the escaped raw text below */
      }
    }
    return "<pre><code>" + escapeHtml(body) + "</code></pre>";
  }

  /**
   * Split a leading YAML front matter block off a source string. marked has no
   * notion of front matter and would render `title:` + `---` as a heading, so we
   * peel it off and show it as metadata instead.
   */
  function splitFrontMatter(source) {
    var match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(source);
    if (match) {
      return { frontMatter: match[1], body: source.slice(match[0].length) };
    }
    return { frontMatter: null, body: source };
  }

  /* ------------------------------------------------------------------ *
   * Detail panel — shows the selected node's category, attributes, and the
   * source it was produced from (raw plus a rendered Markdown preview).
   * ------------------------------------------------------------------ */

  function createDetailPanel() {
    var titleEl = document.getElementById("detail-title");
    var bodyEl = document.getElementById("detail-body");
    var placeholder =
      "<p>Click any node to see the source it was produced from, and a rendered preview.</p>";

    /** Show one node's details (the `data` carried on each display node). */
    function show(data) {
      titleEl.innerHTML = categoryDot(data.category) + escapeHtml(data.label);
      bodyEl.innerHTML = attributesTable(data.attributes) + sourceSection(data.source);
    }

    /** Reset the panel to its empty prompt. */
    function clear() {
      titleEl.textContent = "Node details";
      bodyEl.innerHTML = placeholder;
    }

    // A small colour dot ties the node to its legend colour without repeating a
    // category name (the node's own label already appears beside it).
    function categoryDot(category) {
      if (!category) {
        return "";
      }
      return "<span class=\"dot\" style=\"background:" + colorOf(category) + "\"></span>";
    }

    function attributesTable(attributes) {
      if (!attributes || !attributes.length) {
        return "";
      }
      var rows = attributes
        .map(function (attr) {
          return (
            "<tr><th scope=\"row\">" + escapeHtml(attr.name) + "</th><td>" +
            escapeHtml(attr.value) + "</td></tr>"
          );
        })
        .join("");
      return "<table><tbody>" + rows + "</tbody></table>";
    }

    function sourceSection(source) {
      if (typeof source !== "string") {
        return "";
      }
      return (
        "<h4>Source</h4><pre><code>" + escapeHtml(source) + "</code></pre>" +
        "<h4>Preview</h4><div class=\"preview\">" + renderMarkdown(source) + "</div>"
      );
    }

    return { show: show, clear: clear };
  }

  /* ------------------------------------------------------------------ *
   * Category statistics — how many nodes of each category a stage has, and
   * the distinct node-type names present under it (for the legend labels).
   * ------------------------------------------------------------------ */

  /** @returns {Object<string,{names:string[], count:number}>} keyed by category. */
  function categoryStats(nodes) {
    var stats = {};
    nodes.forEach(function (node) {
      if (!node.category) {
        return;
      }
      var stat = stats[node.category] || (stats[node.category] = { names: [], count: 0 });
      stat.count += 1;
      var name = baseLabel(node.label);
      if (stat.names.indexOf(name) === -1) {
        stat.names.push(name);
      }
    });
    return stats;
  }

  /* ------------------------------------------------------------------ *
   * Tree view — renders one stage as an interactive, collapsible D3 tree.
   *
   * Child edges form the tree; reference edges (shared nodes, cycles) are
   * dashed overlays. Clicking a node selects it (and reports it via onSelect);
   * clicking its circle also collapses/expands it. Arrow keys navigate.
   * ------------------------------------------------------------------ */

  /**
   * @param {{title:string, nodes:Array, edges:Array}} stage
   * @param {(data:object)=>void} onSelect - called with a node's data on select
   * @returns {{svg:SVGElement, legend:HTMLElement, handleKey:Function, clearSelection:Function}}
   */
  function createTreeView(stage, onSelect) {
    var root = stratify(stage);
    root.each(function (node) {
      node._children = node.children;
    });

    var selected = null;
    var dimmedCategories = {}; // category -> true when toggled off in the legend

    var svg = d3.create("svg").attr("class", "tree");
    var viewport = svg.append("g");
    var gLinks = viewport.append("g");
    var gReferences = viewport.append("g");
    var gNodes = viewport.append("g");

    var ratioLabel = null; // the middle button; assigned when controls are built

    var zoom = d3
      .zoom()
      .scaleExtent([0.1, 3])
      // Use the container size as the zoom extent. This also avoids d3's default,
      // which reads the SVG's intrinsic width/height and centres zoom correctly.
      .extent(function () {
        var size = viewportSize();
        return [[0, 0], [size.width, size.height]];
      })
      .on("zoom", function (event) {
        viewport.attr("transform", event.transform);
        updateRatio(event.transform.k);
      });
    svg.call(zoom);

    var controls = buildZoomControls();

    // Vertical gap (46 -> 62) leaves room for nodes with up to three attribute
    // lines (image, link) without overlapping their siblings.
    var layout = d3.tree().nodeSize([62, 220]);
    var diagonal = d3
      .linkHorizontal()
      .x(function (d) {
        return d.y;
      })
      .y(function (d) {
        return d.x;
      });

    update();
    fitToViewport();

    return {
      svg: svg.node(),
      legend: buildLegend(),
      controls: controls,
      handleKey: handleKey,
      clearSelection: function () {
        selected = null;
        applySelection();
      },
    };

    /* --- hierarchy --- */

    function stratify(stage) {
      var parentOf = new Map();
      stage.edges.forEach(function (edge) {
        if (edge.kind !== "Reference") {
          parentOf.set(edge.toId, edge.fromId);
        }
      });
      return d3
        .stratify()
        .id(function (d) {
          return d.id;
        })
        .parentId(function (d) {
          return parentOf.get(d.id) || null;
        })(stage.nodes);
    }

    function referenceEdges() {
      return stage.edges.filter(function (edge) {
        return edge.kind === "Reference";
      });
    }

    /* --- selection --- */

    function select(node) {
      selected = node;
      applySelection();
      onSelect(node.data);
    }

    function applySelection() {
      gNodes.selectAll("g.node").classed("selected", function (d) {
        return d === selected;
      });
    }

    /* --- legend & category filter --- */

    /**
     * Build the interactive legend: one row per category present, showing its
     * colour, the stage's own type name(s), and a node count. Clicking a row
     * toggles that category — dimming its label and all its nodes in the graph.
     */
    function buildLegend() {
      var stats = categoryStats(stage.nodes);
      var legend = document.createElement("div");
      legend.className = "legend";
      Object.keys(CATEGORY_COLORS).forEach(function (category) {
        var stat = stats[category];
        if (stat) {
          legend.appendChild(legendItem(category, stat.names.join(" / "), stat.count));
        }
      });
      return legend;
    }

    function legendItem(category, typeName, count) {
      var item = document.createElement("button");
      item.type = "button";
      item.className = "legend-item";
      item.setAttribute("aria-pressed", "true");

      var swatch = document.createElement("span");
      swatch.className = "swatch";
      swatch.style.background = CATEGORY_COLORS[category];

      var label = document.createElement("span");
      label.className = "legend-label";
      label.textContent = typeName;

      var countEl = document.createElement("span");
      countEl.className = "count";
      countEl.textContent = count;

      item.appendChild(swatch);
      item.appendChild(label);
      item.appendChild(countEl);
      item.addEventListener("click", function () {
        toggleCategory(category, item);
      });
      // Hover (or keyboard focus) highlights every node of this category.
      item.addEventListener("mouseenter", function () {
        highlightCategory(category);
      });
      item.addEventListener("focus", function () {
        highlightCategory(category);
      });
      item.addEventListener("mouseleave", clearHighlight);
      item.addEventListener("blur", clearHighlight);
      return item;
    }

    function highlightCategory(category) {
      gNodes.selectAll("g.node").classed("highlight", function (d) {
        return d.data.category === category;
      });
    }

    function clearHighlight() {
      gNodes.selectAll("g.node").classed("highlight", false);
    }

    function toggleCategory(category, item) {
      var dimmed = !dimmedCategories[category];
      dimmedCategories[category] = dimmed;
      item.classList.toggle("muted", dimmed);
      item.setAttribute("aria-pressed", String(!dimmed));
      applyCategoryFilter();
    }

    /** Dim every node whose category is currently toggled off. */
    function applyCategoryFilter() {
      gNodes.selectAll("g.node").classed("dimmed", function (d) {
        return !!dimmedCategories[d.data.category];
      });
    }

    /* --- collapse / expand --- */

    function toggle(node) {
      node.children = node.children ? null : node._children;
      update();
    }

    function expand(node) {
      if (!node.children && node._children) {
        node.children = node._children;
        update();
      }
    }

    /* --- keyboard navigation --- */

    function handleKey(event) {
      var navigationKeys = ["ArrowRight", "ArrowLeft", "ArrowUp", "ArrowDown", "Enter", " "];
      if (navigationKeys.indexOf(event.key) === -1) {
        return;
      }
      event.preventDefault();

      if (!selected) {
        select(root);
        fitToViewport();
        return;
      }

      if (event.key === "Enter" || event.key === " ") {
        toggle(selected);
        applySelection();
        return;
      }

      var next = nextNode(event.key);
      if (next) {
        select(next);
        centerOn(next);
      }
    }

    function nextNode(key) {
      if (key === "ArrowRight") {
        expand(selected);
        return selected.children ? selected.children[0] : null;
      }
      if (key === "ArrowLeft") {
        return selected.parent || null;
      }
      if (key === "ArrowDown") {
        return sibling(selected, 1);
      }
      if (key === "ArrowUp") {
        return sibling(selected, -1);
      }
      return null;
    }

    function sibling(node, offset) {
      if (!node.parent) {
        return null;
      }
      var siblings = node.parent.children;
      return siblings[siblings.indexOf(node) + offset] || null;
    }

    /* --- rendering --- */

    function update() {
      layout(root);
      var nodes = root.descendants();
      var positionById = new Map(
        nodes.map(function (d) {
          return [d.id, d];
        })
      );

      gLinks
        .selectAll("path.link")
        .data(root.links(), function (d) {
          return d.target.id;
        })
        .join("path")
        .attr("class", "link")
        .attr("d", diagonal);

      gReferences
        .selectAll("path.reference")
        .data(
          referenceEdges().filter(function (r) {
            return positionById.has(r.fromId) && positionById.has(r.toId);
          }),
          function (r) {
            return r.fromId + "->" + r.toId;
          }
        )
        .join("path")
        .attr("class", "link reference")
        .attr("d", function (r) {
          return diagonal({ source: positionById.get(r.fromId), target: positionById.get(r.toId) });
        });

      var node = gNodes.selectAll("g.node").data(nodes, function (d) {
        return d.id;
      });
      node.exit().remove();
      appendEnteringNodes(node.enter());

      gNodes
        .selectAll("g.node")
        .attr("transform", function (d) {
          return "translate(" + d.y + "," + d.x + ")";
        })
        .classed("collapsed", function (d) {
          return !d.children && d._children;
        });

      applySelection();
      applyCategoryFilter();
    }

    function appendEnteringNodes(enter) {
      var group = enter
        .append("g")
        .attr("class", "node")
        .attr("data-tip", function (d) {
          return tooltipHtml(d.data);
        });

      group
        .append("circle")
        .attr("r", 5)
        .style("fill", function (d) {
          return colorOf(d.data.category);
        })
        .on("click", function (event, d) {
          toggle(d);
          select(d);
        });

      group
        .append("text")
        .attr("class", "label")
        .attr("dy", "0.32em")
        .attr("x", 12)
        .text(function (d) {
          return d.data.label;
        });

      group.each(function (d) {
        var attributes = d.data.attributes || [];
        var self = d3.select(this);
        attributes.forEach(function (attr, i) {
          self
            .append("text")
            .attr("class", "attr")
            .attr("x", 12)
            .attr("dy", 15 + i * 12)
            .text(ellipsize(attr.name + ": " + attr.value, MAX_INLINE_TEXT));
        });
      });

      // A generous transparent hit area behind the label and attributes, so the
      // whole node block (not just the tiny circle) is clickable to inspect. Its
      // size is estimated from the (ellipsised) text rather than measured, so it
      // never depends on getBBox (which some SVG engines compute lazily or not).
      group.each(function (d) {
        var lines = [d.data.label].concat(
          (d.data.attributes || []).map(function (attr) {
            return ellipsize(attr.name + ": " + attr.value, MAX_INLINE_TEXT);
          })
        );
        var longest = lines.reduce(function (max, line) {
          return Math.max(max, line.length);
        }, 0);
        var width = 12 + longest * 6.5 + 10;
        var height = 20 + (d.data.attributes || []).length * 12;
        d3.select(this)
          .insert("rect", ":first-child")
          .attr("class", "hit")
          .attr("x", -8)
          .attr("y", -12)
          .attr("width", width)
          .attr("height", height)
          .on("click", function () {
            select(d);
          });
      });
    }

    /* --- camera --- */

    /** Fit the whole tree into the viewport on first render. */
    function fitToViewport() {
      requestAnimationFrame(function () {
        applyFit();
      });
    }

    /** Reset the view to the fitted default (used by the zoom-reset button). */
    function resetView() {
      applyFit();
    }

    /** Compute and apply the fit-to-viewport transform. */
    function applyFit() {
      // Never let an environment quirk (an SVG engine without getBBox or zoom
      // support) break an otherwise-good tree — auto-fit is only a nicety.
      try {
        var bounds = viewport.node().getBBox();
        if (!bounds.width || !bounds.height) {
          return;
        }
        var size = viewportSize();
        var scale = Math.min(
          1,
          0.9 / Math.max(bounds.width / size.width, bounds.height / size.height)
        );
        var tx = size.width / 2 - scale * (bounds.x + bounds.width / 2);
        var ty = size.height / 2 - scale * (bounds.y + bounds.height / 2);
        svg.call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
      } catch {
        /* leave the tree at its default position */
      }
    }

    /** Pan (keeping the current zoom) so a node sits at the viewport centre. */
    function centerOn(node) {
      try {
        var size = viewportSize();
        var transform = d3.zoomTransform(svg.node());
        var tx = size.width / 2 - node.y * transform.k;
        var ty = size.height / 2 - node.x * transform.k;
        svg.call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(transform.k));
      } catch {
        /* centring is optional */
      }
    }

    function viewportSize() {
      var parent = svg.node().parentElement;
      return {
        width: (parent && parent.clientWidth) || 800,
        height: (parent && parent.clientHeight) || 600,
      };
    }

    /* --- zoom controls --- */

    /** Multiply the current zoom by a factor, around the viewport centre. */
    function zoomBy(factor) {
      svg.call(zoom.scaleBy, factor);
    }

    /** Reflect the current scale in the middle button (0.85 -> "85%"). */
    function updateRatio(scale) {
      if (ratioLabel) {
        ratioLabel.textContent = Math.round(scale * 100) + "%";
      }
    }

    /** Build the "− [ratio] +" zoom widget wired to this view's zoom. */
    function buildZoomControls() {
      var container = document.createElement("div");
      container.className = "zoom-controls";

      container.appendChild(
        controlButton("−", "Zoom out", function () {
          zoomBy(1 / ZOOM_STEP);
        })
      );

      ratioLabel = controlButton("100%", "Reset zoom to fit", resetView);
      ratioLabel.classList.add("zoom-ratio");
      container.appendChild(ratioLabel);

      container.appendChild(
        controlButton("+", "Zoom in", function () {
          zoomBy(ZOOM_STEP);
        })
      );
      return container;
    }

    function controlButton(text, ariaLabel, onClick) {
      var button = document.createElement("button");
      button.type = "button";
      button.textContent = text;
      button.title = ariaLabel;
      button.setAttribute("aria-label", ariaLabel);
      button.addEventListener("click", onClick);
      return button;
    }
  }

  /* ------------------------------------------------------------------ *
   * Panel resizer — drag the divider to widen or narrow the detail panel.
   * ------------------------------------------------------------------ */

  function initResizer() {
    var resizer = document.getElementById("resizer");
    var detail = document.getElementById("detail");
    var minWidth = 280;
    var maxWidth = 760;
    var dragging = false;

    resizer.addEventListener("mousedown", function (event) {
      dragging = true;
      document.body.style.userSelect = "none";
      event.preventDefault();
    });

    document.addEventListener("mousemove", function (event) {
      if (!dragging) {
        return;
      }
      var width = window.innerWidth - event.clientX;
      detail.style.flexBasis = Math.max(minWidth, Math.min(maxWidth, width)) + "px";
    });

    document.addEventListener("mouseup", function () {
      dragging = false;
      document.body.style.userSelect = "";
    });
  }

  /* ------------------------------------------------------------------ *
   * Tooltips — rich, accessible hover tooltips (Tippy.js) showing a node's
   * full label and attributes. Delegation covers nodes added later on expand.
   * ------------------------------------------------------------------ */

  function initTooltips(parent) {
    if (!window.tippy) {
      return;
    }
    window.tippy.delegate(parent, {
      target: "g.node",
      allowHTML: true,
      maxWidth: 340,
      delay: [120, 0],
      content: function (reference) {
        return reference.getAttribute("data-tip");
      },
    });
  }

  /* ------------------------------------------------------------------ *
   * App bootstrap — build the tabs and stages, and wire shared interactions.
   * ------------------------------------------------------------------ */

  function initApp() {
    var stages = window.STAGES || [];
    var tabsEl = document.getElementById("tabs");
    var stagesEl = document.getElementById("stages");
    var panel = createDetailPanel();
    var views = [];
    var activeIndex = 0;

    stages.forEach(function (stage, index) {
      tabsEl.appendChild(createTab(stage, index));

      var section = document.createElement("section");
      section.className = "stage";
      stagesEl.appendChild(section);

      try {
        var view = createTreeView(stage, panel.show);
        section.appendChild(view.svg);
        section.appendChild(view.legend);
        section.appendChild(view.controls);
        views.push(view);
      } catch (err) {
        section.classList.add("error");
        section.textContent = "Failed to render stage: " + err.message;
        views.push(null);
      }
    });

    document.addEventListener("keydown", function (event) {
      if (event.target && event.target.closest && event.target.closest("button, input, textarea, select")) {
        return;
      }
      var view = views[activeIndex];
      if (view) {
        view.handleKey(event);
      }
    });

    initResizer();
    initTooltips(stagesEl);
    if (stages.length > 0) {
      activate(0);
    }

    function createTab(stage, index) {
      var tab = document.createElement("button");
      tab.className = "tab";
      tab.type = "button";
      tab.textContent = stage.title;
      tab.addEventListener("click", function () {
        activate(index);
      });
      return tab;
    }

    function activate(index) {
      activeIndex = index;
      forEachChild(tabsEl, function (el, i) {
        el.classList.toggle("active", i === index);
      });
      forEachChild(stagesEl, function (el, i) {
        el.classList.toggle("active", i === index);
      });
      views.forEach(function (view) {
        if (view) {
          view.clearSelection();
        }
      });
      panel.clear();
    }
  }

  function forEachChild(parent, fn) {
    Array.prototype.forEach.call(parent.children, fn);
  }

  initApp();
})();
