using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SceneBuilderTests
{
    [Fact]
    public void Build_EmptyDocument_IsAnEmptyRoot()
    {
        var (root, anchors) = Build();

        Assert.Null(root.Heading);
        Assert.Empty(root.Children);
        Assert.Empty(root.Blocks);
        Assert.False(anchors.TryResolve("anything", out _));
    }

    [Fact]
    public void Build_BlocksBeforeTheFirstHeading_OwnedByTheRoot()
    {
        var leading = Line(Text("intro"));

        var (root, _) = Build(leading, SceneHeading("Scene", 1));

        Assert.Equal([leading], root.Blocks);
        Assert.Single(root.Children);
    }

    [Fact]
    public void Build_TopLevelHeadings_AreRootChildren()
    {
        var (root, _) = Build(SceneHeading("A", 1), SceneHeading("B", 1));

        Assert.Equal(["a", "b"], root.Children.Select(scene => scene.Anchor));
    }

    [Fact]
    public void Build_DeeperHeading_NestsUnderThePrecedingShallowerOne()
    {
        var (root, _) = Build(SceneHeading("A", 1), SceneHeading("A one", 2));

        var a = Assert.Single(root.Children);
        var childAnchors = a.Children.Select(scene => scene.Anchor);
        Assert.Equal(["a-one"], childAnchors);
    }

    [Fact]
    public void Build_ShallowerHeading_ClosesDeeperScenes()
    {
        // ## A  /  ### A one  /  ## B  → A and B are siblings under root; A one nests in A.
        var (root, _) = Build(
            SceneHeading("A", 2),
            SceneHeading("A one", 3),
            SceneHeading("B", 2));

        Assert.Equal(["a", "b"], root.Children.Select(scene => scene.Anchor));
        Assert.Equal(["a-one"], root.Children[0].Children.Select(scene => scene.Anchor));
    }

    [Fact]
    public void Build_AScene_OwnsTheBlocksUntilTheNextHeading()
    {
        var inA = Line(Text("in a"));
        var inB = Line(Text("in b"));

        var (root, _) = Build(
            SceneHeading("A", 1), inA,
            SceneHeading("B", 1), inB);

        Assert.Equal([inA], root.Children[0].Blocks);
        Assert.Equal([inB], root.Children[1].Blocks);
    }

    [Fact]
    public void Build_IndexesEachSceneByItsAnchor()
    {
        var (_, anchors) = Build(SceneHeading("Play tennis", 1));

        Assert.True(anchors.TryResolve("play-tennis", out var scene));
        Assert.Equal("play-tennis", scene.Anchor);
    }

    [Fact]
    public void Build_DuplicateAnchor_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(
            () => Build(SceneHeading("Play tennis", 1), SceneHeading("Play Tennis", 2)));

        Assert.Contains("#play-tennis", error.Message);
    }

    [Fact]
    public void Build_HeadingThatSlugsToNothing_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(() => Build(SceneHeading("!!!", 1)));

        Assert.Contains("letter or number", error.Message);
    }

    private static (Scene Root, AnchorTable Anchors) Build(params ScriptBlock[] blocks) =>
        SceneBuilder.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)));
}
