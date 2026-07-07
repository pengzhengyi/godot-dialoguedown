using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class HtmlRendererTests
{
    private readonly HtmlRenderer _renderer = new();

    [Fact]
    public void Render_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.Render(null!));
    }

    [Fact]
    public void Render_ProducesSelfContainedOfflineDocument()
    {
        var graph = Graph("Markdown AST", [Node("n0", "Document")], []);

        var html = _renderer.Render(graph);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("d3js.org v7.9.0", html);   // D3 is inlined, not linked
        Assert.DoesNotContain("<script src", html); // no external resources
    }

    [Fact]
    public void Render_EmbedsStageTitleAndNodeLabels()
    {
        var graph = Graph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Paragraph")],
            [Child("n0", "n1")]);

        var html = _renderer.Render(graph);

        Assert.Contains("\"title\":\"Markdown AST\"", html);
        Assert.Contains("\"label\":\"Paragraph\"", html);
    }

    [Fact]
    public void Render_EscapesScriptClosingTagFromData()
    {
        var graph = Graph("G", [Node("n0", "</script>")], []);

        var html = _renderer.Render(graph);

        // The data-derived closing tag is unicode-escaped inside the inlined JSON.
        Assert.Contains("\\u003C/script", html);
    }
}
