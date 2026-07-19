using DialogueDown.Diagnostics;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class AnchorTableTests
{
    [Fact]
    public void TryResolve_FindsAnAddedScene()
    {
        var table = new AnchorTable();
        var scene = Scene.ForHeading(SceneHeading("Play tennis", 1), "play-tennis");
        table.Add("play-tennis", scene, SourceSpanFactory.Span(), new DiagnosticBag());

        Assert.True(table.TryResolve("play-tennis", out var resolved));
        Assert.Same(scene, resolved);
    }

    [Fact]
    public void TryResolve_ReturnsFalseForAnUnknownAnchor()
    {
        var table = new AnchorTable();

        Assert.False(table.TryResolve("missing", out var resolved));
        Assert.Null(resolved);
    }

    [Fact]
    public void Add_DuplicateAnchor_ReportsAndKeepsTheFirst()
    {
        var table = new AnchorTable();
        var first = Scene.ForHeading(SceneHeading("Play tennis", 1), "play-tennis");
        var second = Scene.ForHeading(SceneHeading("Play Tennis", 2), "play-tennis");
        var diagnostics = new DiagnosticBag();
        table.Add("play-tennis", first, SourceSpanFactory.Span(), diagnostics);

        table.Add("play-tennis", second, SourceSpanFactory.Span(), diagnostics);

        AssertReported(diagnostics.Diagnostics, "DLG2001");
        Assert.True(table.TryResolve("play-tennis", out var resolved));
        Assert.Same(first, resolved);
    }
}
