using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class JumpResolverTests
{
    [Fact]
    public void Resolve_SameFileAnchor_ResolvesToItsScene()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));

        var resolution = ResolveOne(Jump("#play-tennis"), anchors);

        var sceneJump = Assert.IsType<SceneJump>(resolution);
        Assert.Equal("play-tennis", sceneJump.Scene.Anchor);
    }

    [Fact]
    public void Resolve_SameFileAnchor_MissingScene_Throws()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));

        var error = Assert.Throws<DialogueSemanticError>(
            () => ResolveOne(Jump("#no-such-scene"), anchors));

        Assert.Contains("#no-such-scene", error.Message);
    }

    [Fact]
    public void Resolve_FileScopedTarget_IsFileScoped()
    {
        var resolution = ResolveOne(Jump("chapter-02.md#meet-bob"), new AnchorTable());

        var fileScoped = Assert.IsType<FileScopedJump>(resolution);
        Assert.Equal("chapter-02.md", fileScoped.File);
        Assert.Equal("meet-bob", fileScoped.Anchor);
    }

    [Fact]
    public void Resolve_FileScopedWithoutAnchor_HasNoAnchor()
    {
        var fileScoped = Assert.IsType<FileScopedJump>(ResolveOne(Jump("chapter-02.md"), new AnchorTable()));

        Assert.Equal("chapter-02.md", fileScoped.File);
        Assert.Null(fileScoped.Anchor);
    }

    [Fact]
    public void Resolve_UrlTarget_IsFileScoped_NotAnError()
    {
        var fileScoped = Assert.IsType<FileScopedJump>(
            ResolveOne(Jump("http://example.com"), new AnchorTable()));

        Assert.Equal("http://example.com", fileScoped.File);
    }

    [Fact]
    public void Resolve_EmptyTarget_IsUnresolved() =>
        Assert.IsType<UnresolvedJump>(ResolveOne(Jump(string.Empty), new AnchorTable()));

    [Fact]
    public void Resolve_HashOnlyTarget_IsUnresolved() =>
        Assert.IsType<UnresolvedJump>(ResolveOne(Jump("#"), new AnchorTable()));

    [Fact]
    public void Resolve_KeysEachResolutionByItsJump()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));
        var scene = Jump("#play-tennis");
        var external = Jump("chapter-02.md");

        var resolutions = JumpResolver.Resolve([scene, external], anchors);

        Assert.IsType<SceneJump>(resolutions[scene]);
        Assert.IsType<FileScopedJump>(resolutions[external]);
    }

    private static JumpResolution ResolveOne(Jump jump, AnchorTable anchors) =>
        JumpResolver.Resolve([jump], anchors)[jump];

    private static AnchorTable AnchorsFor(params ScriptBlock[] blocks)
    {
        var (_, anchors) = SceneBuilder.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)));
        return anchors;
    }
}
