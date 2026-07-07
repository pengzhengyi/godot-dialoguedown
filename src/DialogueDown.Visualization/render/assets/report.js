/* Renders each compiler stage in window.STAGES as an interactive D3 tree.
 * Child edges form the tree; reference edges (shared nodes, cycles) are drawn as
 * dashed overlays. Click a node to collapse or expand; drag to pan; scroll to zoom.
 */
(function () {
  "use strict";

  var stages = window.STAGES || [];
  var tabsEl = document.getElementById("tabs");
  var stagesEl = document.getElementById("stages");

  stages.forEach(function (stage, index) {
    var tab = document.createElement("button");
    tab.className = "tab";
    tab.textContent = stage.title;
    tab.addEventListener("click", function () {
      activate(index);
    });
    tabsEl.appendChild(tab);

    var section = document.createElement("section");
    section.className = "stage";
    stagesEl.appendChild(section);
    try {
      section.appendChild(renderTree(stage));
    } catch (err) {
      section.classList.add("error");
      section.textContent = "Failed to render stage: " + err.message;
    }
  });

  if (stages.length > 0) {
    activate(0);
  }

  function activate(index) {
    Array.prototype.forEach.call(tabsEl.children, function (el, i) {
      el.classList.toggle("active", i === index);
    });
    Array.prototype.forEach.call(stagesEl.children, function (el, i) {
      el.classList.toggle("active", i === index);
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

    return svg.node();

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
        });

      enter
        .append("text")
        .attr("dy", "0.32em")
        .attr("x", 9)
        .text(function (d) {
          return d.data.label;
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
