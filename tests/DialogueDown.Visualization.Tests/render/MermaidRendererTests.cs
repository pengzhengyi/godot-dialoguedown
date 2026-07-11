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
        var graph = MakeGraph(
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
        var graph = MakeGraph("G", [Node("n0", "a"), Node("n1", "b")], [Reference("n0", "n1")]);

        var output = _renderer.Render(graph);

        Assert.Contains("n0 -.-> n1", output);
        Assert.DoesNotContain("-->", output);
    }

    [Fact]
    public void Render_IncludesAttributesAsExtraLines()
    {
        var graph = MakeGraph(
            "G",
            [Node("n0", "Heading", Attr("level", "2"), Attr("span", "[0, 8)"))],
            []);

        var output = _renderer.Render(graph);

        Assert.Contains("n0[\"Heading<br/>level: 2<br/>span: [0, 8)\"]", output);
    }

    [Fact]
    public void Render_EscapesHtmlSpecialCharactersInLabel()
    {
        var graph = MakeGraph("G", [Node("n0", "a <b> & \"c\"")], []);

        var output = _renderer.Render(graph);

        Assert.Contains("n0[\"a &lt;b&gt; &amp; &quot;c&quot;\"]", output);
    }

    [Fact]
    public void Render_CategorizedNode_TagsItAndEmitsAClassDefWithTheCategoryColor()
    {
        var graph = MakeGraph("G", [NodeWithCategory("n0", "Line", "speech")], []);

        var output = _renderer.Render(graph);

        Assert.Contains("n0[\"Line\"]:::catspeech", output);
        Assert.Contains("classDef catspeech fill:#22c55e,color:#fff,stroke:#0f172a", output);
    }

    [Fact]
    public void Render_PrefixesClassNames_SoAReservedCategoryLikeCallDoesNotBreakMermaid()
    {
        // Mermaid reserves the bare word `call`; the `cat` prefix keeps the class valid.
        var graph = MakeGraph("G", [NodeWithCategory("n0", "Command", "call")], []);

        var output = _renderer.Render(graph);

        Assert.Contains(":::catcall", output);
        Assert.DoesNotContain(":::call\n", output);
    }

    [Fact]
    public void Render_EmitsEachCategoryClassDefOnce_EvenWithRepeatedNodes()
    {
        var graph = MakeGraph(
            "G",
            [NodeWithCategory("n0", "a", "text"), NodeWithCategory("n1", "b", "text")],
            [Child("n0", "n1")]);

        var output = _renderer.Render(graph);

        var occurrences = output.Split("classDef cattext").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Render_UncategorizedNode_StaysPlain()
    {
        var graph = MakeGraph("G", [Node("n0", "a")], []);

        var output = _renderer.Render(graph);

        Assert.DoesNotContain(":::", output);
        Assert.DoesNotContain("classDef", output);
    }
}
