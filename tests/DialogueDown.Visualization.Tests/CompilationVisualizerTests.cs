using DialogueDown.Markdown;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests;

public sealed class CompilationVisualizerTests
{
    [Fact]
    public void Constructor_NullParser_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationVisualizer(null!));
    }

    [Fact]
    public void BuildStages_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationVisualizer().BuildStages(null!));
    }

    [Fact]
    public void BuildStages_ProducesMarkdownAstStageFromInjectedParser()
    {
        var document = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var parser = new StubMarkdownParser(document);
        var visualizer = new CompilationVisualizer(parser);

        var stages = visualizer.BuildStages("script source");

        var stage = Assert.Single(stages);
        Assert.Equal("Markdown AST", stage.Title);
        Assert.Equal("script source", parser.ReceivedSource);
        Assert.Contains(stage.Nodes, n => n.Label == "Paragraph");
    }

    [Fact]
    public void RenderHtmlReport_RealParser_ProducesSelfContainedReportWithStageAndLabels()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport(
            """
            # Hello

            World
            """);

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn.jsdelivr.net", html);    // self-contained: no CDN
        Assert.Contains(".tippy-box", html);                // client libs inlined
        Assert.Contains("\"title\":\"Markdown AST\"", html);
        Assert.Contains("Heading (H1)", html);
        Assert.Contains("Paragraph", html);
        Assert.Contains("\"source\":\"# Hello\"", html);     // heading's source snippet
        Assert.Contains("\"source\":\"# Hello\\n\\nWorld\"", html); // whole document for the Source tab
    }
}
