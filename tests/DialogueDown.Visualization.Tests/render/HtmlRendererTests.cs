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
    public void Render_PrefersCdnWithOfflineFallbackForEachLibrary()
    {
        var graph = Graph("Markdown AST", [Node("n0", "Document")], []);

        var html = _renderer.Render(graph);

        Assert.StartsWith("<!DOCTYPE html>", html);
        // Each library is requested from a CDN and also inlined as an offline fallback.
        Assert.Contains("cdn.jsdelivr.net/npm/d3@7", html);
        Assert.Contains("d3js.org v7.9.0", html);
        Assert.Contains("cdn.jsdelivr.net/npm/@picocss/pico@2", html);
        Assert.Contains("Pico CSS", html);
        Assert.Contains("cdn.jsdelivr.net/npm/marked@12", html);
        Assert.Contains("marked v12.0.2", html);
    }

    [Fact]
    public void Render_EmbedsNodeSourceSnippet()
    {
        var graph = Graph("Markdown AST", [new DisplayNode("n0", "Heading (H1)", [], "# Hello")], []);

        var html = _renderer.Render(graph);

        Assert.Contains("\"source\":\"# Hello\"", html);
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
