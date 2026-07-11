using System.Text;

namespace DialogueDown.Visualization;

/// <summary>
/// Renders a display graph as <see href="https://mermaid.js.org/">Mermaid</see>
/// <c>flowchart</c> text, which renders inline on GitHub and in many editors.
/// Child edges are solid arrows; reference edges are dotted. Nodes are colored by
/// their semantic category (via a <c>classDef</c> per category present), matching
/// the interactive report. Labels and their attributes are HTML-escaped, since
/// Mermaid treats node text as HTML.
/// </summary>
public sealed class MermaidRenderer : IDisplayRenderer
{
    public string Render(DisplayGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var builder = new StringBuilder();
        builder.Append("flowchart TD\n");

        foreach (var node in graph.Nodes)
        {
            builder.Append("    ").Append(node.Id)
                .Append("[\"").Append(NodeText(node)).Append("\"]")
                .Append(ClassSuffix(node))
                .Append('\n');
        }

        foreach (var edge in graph.Edges)
        {
            var connector = edge.Kind == DisplayEdgeKind.Reference ? " -.-> " : " --> ";
            builder.Append("    ").Append(edge.FromId).Append(connector).Append(edge.ToId).Append('\n');
        }

        AppendClassDefs(builder, graph);
        return builder.ToString();
    }

    // Tag a node with its category class so the classDef below colors it. The class
    // name is prefixed (`cat…`) so a category like "call" cannot collide with a Mermaid
    // reserved word (which would break parsing).
    private static string ClassSuffix(DisplayNode node) =>
        node.Category is null ? string.Empty : $":::{ClassName(node.Category)}";

    private static string ClassName(string category) => "cat" + category;

    // One classDef per distinct category present, in first-seen order, coloring the
    // nodes to match the report's legend.
    private static void AppendClassDefs(StringBuilder builder, DisplayGraph graph)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            if (node.Category is not null && seen.Add(node.Category))
            {
                builder.Append("    classDef ").Append(ClassName(node.Category))
                    .Append(" fill:").Append(CategoryPalette.ColorOf(node.Category))
                    .Append(",color:#fff,stroke:#0f172a\n");
            }
        }
    }

    private static string NodeText(DisplayNode node)
    {
        var lines = new List<string> { node.Label };
        lines.AddRange(node.Attributes.Select(a => $"{a.Name}: {a.Value}"));
        return string.Join("<br/>", lines.Select(Escape));
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("\"", "&quot;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\r\n", "<br/>")
        .Replace("\n", "<br/>")
        .Replace("\r", "<br/>");
}
