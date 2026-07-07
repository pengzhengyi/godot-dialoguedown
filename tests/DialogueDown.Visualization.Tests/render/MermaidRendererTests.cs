using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class MermaidRendererTests
{
    private readonly MermaidRenderer _renderer = new();

    [Fact]
    public void Render_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.Render(null!));
    }

    [Fact]
    public void Render_TreeGraph_EmitsFlowchartNodesAndChildEdge()
    {
        var graph = Graph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Paragraph")],
            [Child("n0", "n1")]);

        var output = _renderer.Render(graph);

        Assert.Equal(
            """
            flowchart TD
                n0["Document"]
                n1["Paragraph"]
                n0 --> n1

            """,
            output);
    }

    [Fact]
    public void Render_ReferenceEdge_UsesDottedConnector()
    {
        var graph = Graph("G", [Node("n0", "a"), Node("n1", "b")], [Reference("n0", "n1")]);

        var output = _renderer.Render(graph);

        Assert.Contains("n0 -.-> n1", output);
        Assert.DoesNotContain("-->", output);
    }

    [Fact]
    public void Render_IncludesAttributesAsExtraLines()
    {
        var graph = Graph(
            "G",
            [Node("n0", "Heading", Attr("level", "2"), Attr("span", "[0, 8)"))],
            []);

        var output = _renderer.Render(graph);

        Assert.Contains("n0[\"Heading<br/>level: 2<br/>span: [0, 8)\"]", output);
    }

    [Fact]
    public void Render_EscapesHtmlSpecialCharactersInLabel()
    {
        var graph = Graph("G", [Node("n0", "a <b> & \"c\"")], []);

        var output = _renderer.Render(graph);

        Assert.Contains("n0[\"a &lt;b&gt; &amp; &quot;c&quot;\"]", output);
    }
}
