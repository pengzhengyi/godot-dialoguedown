using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Semantics;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SceneEntityTests
{
    [Fact]
    public void Key_PrefixesTheAnchor() =>
        Assert.Equal("scene:the-market", SceneEntity.Key(ForHeading("The Market", "the-market")));

    [Fact]
    public void Label_IsTheHeadingText() =>
        Assert.Equal("The Market", SceneEntity.Label(ForHeading("The Market", "the-market")));

    [Fact]
    public void Label_FallsBackToTheAnchorWhenHeadingTextIsEmpty()
    {
        var scene = Scene.ForHeading(new SceneHeading([], 1, new SourceSpan(0, 1)), "s");
        Assert.Equal("#s", SceneEntity.Label(scene));
    }

    private static Scene ForHeading(string title, string anchor, int level = 1) =>
        Scene.ForHeading(new SceneHeading([new Text(title, new SourceSpan(0, 1))], level, new SourceSpan(0, 1)), anchor);
}
