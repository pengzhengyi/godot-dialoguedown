using DialogueDown.Common;
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
    public void BuildStages_ProducesMarkdownAndDialogueStagesFromInjectedParser()
    {
        var document = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var parser = new StubMarkdownParser(document);
        var visualizer = new CompilationVisualizer(parser);

        var stages = visualizer.BuildStages("script source");

        Assert.Collection(
            stages,
            markdown => Assert.Equal("Markdown AST", markdown.Title),
            dialogue => Assert.Equal("Dialogue AST", dialogue.Title));
        Assert.Equal("script source", parser.ReceivedSource);
        Assert.Contains(stages[0].Nodes, n => n.Label == "Paragraph");
        Assert.Contains(stages[1].Nodes, n => n.Label == "Line"); // the paragraph becomes a line
    }

    [Fact]
    public void BuildStages_RealParser_ProducesDialogueStageWithSpeakersChoicesAndCalls()
    {
        var stages = new CompilationVisualizer().BuildStages(
            """
            # Scene

            Alice: Hello, **there**! `Wave()`

            - Go left
            - Go right
            """);

        var dialogue = Assert.IsType<DisplayGraph>(stages[1]);
        Assert.Equal("Dialogue AST", dialogue.Title);
        Assert.False(string.IsNullOrWhiteSpace(dialogue.Description));
        Assert.Contains(dialogue.Nodes, n => n.Label == "Line");
        Assert.Contains(dialogue.Nodes, n => n.Label.StartsWith("Speaker", StringComparison.Ordinal));
        Assert.Contains(dialogue.Nodes, n => n.Label.StartsWith("Choices", StringComparison.Ordinal));
        Assert.Contains(dialogue.Nodes, n => n.Category == "call");     // the `Wave()` game call
        Assert.Contains(dialogue.Nodes, n => n.Category == "styling");  // the **there** bold
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

    [Fact]
    public void RenderLiveReport_MarksThePayloadWithTheModeAndDocumentPath()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderLiveReport("scene.dialogue.md", "# Hello", "view");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"view\"", html);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", html);
        Assert.Contains("\"title\":\"Markdown AST\"", html);
    }

    [Fact]
    public void RenderHtmlReport_MarksThePayloadStatic()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Hello");

        Assert.Contains("\"mode\":\"static\"", html);
        Assert.DoesNotContain("\"mode\":\"view\"", html);
    }

    [Fact]
    public void RenderHtmlReport_WithDocumentPath_IncludesThePathButStaysStatic()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Hello", "scene.dialogue.md");

        Assert.Contains("\"mode\":\"static\"", html);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", html);
    }

    [Fact]
    public void RenderLiveReport_NullDocumentPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().RenderLiveReport(null!, "# Hello", "view"));
    }

    [Fact]
    public void SerializeDocument_ReturnsModePathSourceAndStages()
    {
        var visualizer = new CompilationVisualizer();

        var json = visualizer.SerializeDocument("scene.dialogue.md", "# Hello", "view");

        Assert.Contains("\"mode\":\"view\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hello\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.Contains("\"title\":\"Markdown AST\"", json);
    }

    [Fact]
    public void SerializeDocument_NullDocumentPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().SerializeDocument(null!, "# Hello", "view"));
    }

    [Fact]
    public void LocalImageReferences_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().LocalImageReferences(null!));
    }

    [Fact]
    public void LocalImageReferences_ReturnsImageSourcesInDocumentOrder()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            """
            Alice: This is *your* photo. ![Bob's photo](assets/bob.jpg)

            - Bob: And a painting. ![Painting](../shared/painting.png)
            """);

        Assert.Equal(["assets/bob.jpg", "../shared/painting.png"], references);
    }

    [Fact]
    public void LocalImageReferences_SkipsWebAndDataUrls()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            """
            ![remote](https://example.com/a.png)
            ![protocol](//example.com/b.png)
            ![data](data:image/png;base64,AAAA)
            ![local](assets/c.png)
            """);

        Assert.Equal(["assets/c.png"], references);
    }

    [Fact]
    public void LocalImageReferences_IncludesAbsoluteFilesystemPaths()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            "![outside](/var/gallery/painting.jpg)");

        Assert.Equal(["/var/gallery/painting.jpg"], references);
    }

    [Fact]
    public void LocalImageReferences_NoImages_ReturnsEmpty()
    {
        var references = new CompilationVisualizer().LocalImageReferences("# Just a heading\n\nAlice: Hi.");

        Assert.Empty(references);
    }
}
