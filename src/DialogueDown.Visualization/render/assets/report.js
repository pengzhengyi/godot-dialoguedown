/* Renders each compiler stage in window.STAGES as an interactive D3 tree, and
 * shows a clicked node's details — attributes, the source it was produced from,
 * and a rendered Markdown preview of that source — in the side panel.
 *
 * Child edges form the tree; reference edges (shared nodes, cycles) are dashed
 * overlays. Click a node to select it; click a node's circle to collapse/expand.
 */
(function () {
  "use strict";

  var stages = window.STAGES || [];
  var tabsEl = document.getElementById("tabs");
  var stagesEl = document.getElementById("stages");
  var detailTitle = document.getElementById("detail-title");
  var detailBody = document.getElementById("detail-body");

  var trees = [];

  stages.forEach(function (stage, index) {
    var tab = document.createElement("button");
    tab.className = "tab";
    tab.type = "button";
    tab.textContent = stage.title;
    tab.addEventListener("click", function () {
      activate(index);
    });
    tabsEl.appendChild(tab);

    var section = document.createElement("section");
    section.className = "stage";
    stagesEl.appendChild(section);
    try {
      var tree = renderTree(stage);
      section.appendChild(tree.svg);
      trees.push(tree);
    } catch (err) {
      section.classList.add("error");
      section.textContent = "Failed to render stage: " + err.message;
      trees.push(null);
    }
  });

  if (stages.length > 0) {
    activate(0);
  }

  function activate(index) {
    forEachChild(tabsEl, function (el, i) {
      el.classList.toggle("active", i === index);
    });
    forEachChild(stagesEl, function (el, i) {
      el.classList.toggle("active", i === index);
    });
    clearDetail();
  }

  function forEachChild(parent, fn) {
    Array.prototype.forEach.call(parent.children, fn);
  }

  function clearDetail() {
    trees.forEach(function (tree) {
      if (tree) {
        tree.clearSelection();
      }
    });
    detailTitle.textContent = "Node details";
    detailBody.innerHTML =
      "<p>Click any node in the graph to see the source it was produced from, and a rendered preview.</p>";
  }

  function showDetail(data) {
    detailTitle.textContent = data.label;
    var html = "";
    if (data.attributes && data.attributes.length) {
      html += "<table><tbody>";
      data.attributes.forEach(function (attr) {
        html +=
          "<tr><th scope=\"row\">" +
          escapeHtml(attr.name) +
          "</th><td>" +
          escapeHtml(attr.value) +
          "</td></tr>";
      });
      html += "</tbody></table>";
    }
    if (typeof data.source === "string") {
      html += "<h4>Source</h4><pre><code>" + escapeHtml(data.source) + "</code></pre>";
      html += "<h4>Preview</h4><div class=\"preview\">" + renderMarkdown(data.source) + "</div>";
    }
    detailBody.innerHTML = html || "<p>No further detail for this node.</p>";
  }

  function renderMarkdown(source) {
    if (window.marked && typeof window.marked.parse === "function") {
      try {
        return window.marked.parse(source);
      } catch (err) {
        /* fall back to raw text below */
      }
    }
    return "<pre><code>" + escapeHtml(source) + "</code></pre>";
  }

  function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, function (ch) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch];
    });
  }

  function renderTree(stage) {
    var parentOf = new Map();
    var references = [];
    stage.edges.forEach(function (edge) {
      if (edge.kind === "Reference") {
        references.push(edge);
      } else {
        parentOf.set(edge.toId, edge.fromId);
      }
    });

    // Child edges form a spanning tree; stratify them into a hierarchy.
    var root = d3
      .stratify()
      .id(function (d) {
        return d.id;
      })
      .parentId(function (d) {
        return parentOf.get(d.id) || null;
      })(stage.nodes);

    root.each(function (d) {
      d._children = d.children;
    });

    var selected = null;

    var svg = d3.create("svg").attr("class", "tree");
    var g = svg.append("g");
    var gLinks = g.append("g");
    var gRefs = g.append("g");
    var gNodes = g.append("g");

    var zoom = d3
      .zoom()
      .scaleExtent([0.1, 3])
      .on("zoom", function (event) {
        g.attr("transform", event.transform);
      });
    svg.call(zoom);

    var layout = d3.tree().nodeSize([46, 220]);
    var diagonal = d3
      .linkHorizontal()
      .x(function (d) {
        return d.y;
      })
      .y(function (d) {
        return d.x;
      });

    update();
    center();

    return {
      svg: svg.node(),
      clearSelection: function () {
        selected = null;
        applySelection();
      },
    };

    function select(d) {
      selected = d;
      applySelection();
      showDetail(d.data);
    }

    function applySelection() {
      gNodes.selectAll("g.node").classed("selected", function (d) {
        return d === selected;
      });
    }

    function update() {
      layout(root);
      var nodes = root.descendants();
      var position = new Map(
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

      gRefs
        .selectAll("path.reference")
        .data(
          references.filter(function (r) {
            return position.has(r.fromId) && position.has(r.toId);
          }),
          function (r) {
            return r.fromId + "->" + r.toId;
          }
        )
        .join("path")
        .attr("class", "link reference")
        .attr("d", function (r) {
          return diagonal({ source: position.get(r.fromId), target: position.get(r.toId) });
        });

      var node = gNodes.selectAll("g.node").data(nodes, function (d) {
        return d.id;
      });

      var enter = node
        .enter()
        .append("g")
        .attr("class", "node");

      enter
        .append("circle")
        .attr("r", 5)
        .on("click", function (event, d) {
          d.children = d.children ? null : d._children;
          update();
          select(d);
        });

      enter
        .append("text")
        .attr("dy", "0.32em")
        .attr("x", 9)
        .text(function (d) {
          return d.data.label;
        })
        .on("click", function (event, d) {
          select(d);
        });

      enter.each(function (d) {
        var group = d3.select(this);
        (d.data.attributes || []).forEach(function (attr, i) {
          group
            .append("text")
            .attr("class", "attr")
            .attr("x", 9)
            .attr("dy", 15 + i * 12)
            .text(attr.name + ": " + attr.value);
        });
      });

      enter
        .merge(node)
        .attr("transform", function (d) {
          return "translate(" + d.y + "," + d.x + ")";
        })
        .classed("collapsed", function (d) {
          return !d.children && d._children;
        });

      node.exit().remove();
      applySelection();
    }

    function center() {
      requestAnimationFrame(function () {
        // Auto-centering is a nicety; never let an environment quirk (an SVG
        // engine without getBBox or zoom support) break an otherwise-good tree.
        try {
          var bounds = g.node().getBBox();
          if (!bounds.width || !bounds.height) {
            return;
          }
          var parent = svg.node().parentElement;
          var width = parent.clientWidth || 800;
          var height = parent.clientHeight || 600;
          var scale = Math.min(1, 0.9 / Math.max(bounds.width / width, bounds.height / height));
          var tx = width / 2 - scale * (bounds.x + bounds.width / 2);
          var ty = height / 2 - scale * (bounds.y + bounds.height / 2);
          svg.call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
        } catch (err) {
          /* leave the tree at its default position */
        }
      });
    }
  }
})();
