using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class AnchorTableTests
{
    [Fact]
    public void TryResolve_FindsAnAddedScene()
    {
        var table = new AnchorTable();
        var scene = Scene.ForHeading(SceneHeading("Play tennis", 1), "play-tennis");
        table.Add("play-tennis", scene, SourceSpanFactory.Span());

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
    public void Add_DuplicateAnchor_Throws()
    {
        var table = new AnchorTable();
        var first = Scene.ForHeading(SceneHeading("Play tennis", 1), "play-tennis");
        var second = Scene.ForHeading(SceneHeading("Play Tennis", 2), "play-tennis");
        table.Add("play-tennis", first, SourceSpanFactory.Span());

        var error = Assert.Throws<DialogueSemanticError>(
            () => table.Add("play-tennis", second, SourceSpanFactory.Span()));

        Assert.Contains("#play-tennis", error.Message);
    }
}
