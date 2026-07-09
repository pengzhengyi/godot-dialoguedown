using System.Text;

namespace DialogueDown.Visualization;

/// <summary>
/// Renders a display graph as <see href="https://mermaid.js.org/">Mermaid</see>
/// <c>flowchart</c> text, which renders inline on GitHub and in many editors.
/// Child edges are solid arrows; reference edges are dotted. Labels and their
/// attributes are HTML-escaped, since Mermaid treats node text as HTML.
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
                .Append("[\"").Append(NodeText(node)).Append("\"]\n");
        }

        foreach (var edge in graph.Edges)
        {
            var connector = edge.Kind == DisplayEdgeKind.Reference ? " -.-> " : " --> ";
            builder.Append("    ").Append(edge.FromId).Append(connector).Append(edge.ToId).Append('\n');
        }

        return builder.ToString();
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
