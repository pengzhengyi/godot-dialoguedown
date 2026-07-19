using DialogueDown.Diagnostics;
using Spectre.Console.Testing;

namespace DialogueDown.Cli.Tests;

public sealed class ErrataRendererTests
{
    [Fact]
    public void Render_Plain_WritesEachDiagnosticSortedByPosition_WithASummary()
    {
        var console = PlainConsole();
        var diagnostics = new[]
        {
            Located("DLG2001", DiagnosticSeverity.Error, "duplicate anchor", 3, 1),
            Located("DLG1003", DiagnosticSeverity.Warning, "two jumps", 1, 5),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        var output = console.Output;
        Assert.Contains("scene.dialogue.md(1,5): warning DLG1003: two jumps", output, StringComparison.Ordinal);
        Assert.Contains("scene.dialogue.md(3,1): error DLG2001: duplicate anchor", output, StringComparison.Ordinal);
        // Sorted by position: the line-1 warning is written before the line-3 error.
        Assert.True(
            output.IndexOf("DLG1003", StringComparison.Ordinal)
            < output.IndexOf("DLG2001", StringComparison.Ordinal));
        Assert.Contains("1 error, 1 warning", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Plain_EscapesMarkupInAMessage()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md", "", [Located("DLG1102", DiagnosticSeverity.Error, "'[x]' is not a game call", 1, 1)]);

        // The literal brackets survive rather than being parsed as (invalid) Spectre markup.
        Assert.Contains("'[x]' is not a game call", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NoDiagnostics_WritesNothing()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render("s.dialogue.md", "", []);

        Assert.Equal(string.Empty, console.Output);
    }

    [Fact]
    public void Render_Interactive_RendersRichSourceContext()
    {
        var console = InteractiveConsole();
        const string source = "Alice: say `bad`"; // the code span `bad` at offsets 11..16 is not a game call
        var diagnostics = new[]
        {
            new LocatedDiagnostic(
                "DLG1102", DiagnosticSeverity.Error, "not a game call",
                new LinePosition(1, 12), new LinePosition(1, 17), StartOffset: 11, EndOffset: 16),
        };

        new ErrataRenderer(console).Render("s.dialogue.md", source, diagnostics);

        var output = console.Output;
        Assert.Contains("DLG1102", output, StringComparison.Ordinal);
        Assert.Contains("not a game call", output, StringComparison.Ordinal);
        // Errata draws the offending source line in its block.
        Assert.Contains("Alice: say", output, StringComparison.Ordinal);
    }

    private static TestConsole PlainConsole()
    {
        var console = new TestConsole();
        console.Profile.Width = 300; // avoid wrapping so full lines can be asserted
        console.Profile.Capabilities.Interactive = false;
        return console;
    }

    private static TestConsole InteractiveConsole()
    {
        var console = new TestConsole().Interactive();
        console.Profile.Width = 300;
        return console;
    }

    private static LocatedDiagnostic Located(
        string code, DiagnosticSeverity severity, string message, int line, int column) =>
        new(
            code, severity, message,
            new LinePosition(line, column), new LinePosition(line, column),
            StartOffset: 0, EndOffset: 0);
}
