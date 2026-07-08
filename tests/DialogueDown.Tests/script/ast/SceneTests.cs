using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class SceneTests
{
    [Fact]
    public void Constructor_ExposesTitleLevelBodyAndSpan_AndIsABlock()
    {
        var span = SourceSpanFactory.Span();
        var title = new InlineFragment[] { Text("Greetings") };
        var body = new Block[] { Line(Text("Hello, Bob!")) };

        var scene = new Scene(title, 2, body, span);

        Assert.Equal(title, scene.Title);
        Assert.Equal(2, scene.Level);
        Assert.Equal(body, scene.Body);
        Assert.Equal(span, scene.Span);
        Assert.IsAssignableFrom<Block>(scene);
        Assert.IsAssignableFrom<ScriptNode>(scene);
    }

    [Fact]
    public void Constructor_NestsAScene_ForADeeperHeading()
    {
        var inner = new Scene([Text("Play tennis")], 2, [Line(Text("Ready?"))], SourceSpanFactory.Span());

        var outer = new Scene([Text("Greetings")], 1, [inner], SourceSpanFactory.Span());

        Assert.Same(inner, Assert.IsType<Scene>(outer.Body[0]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Constructor_LevelOutsideOneToSix_Throws(int level) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Scene([Text("t")], level, [], SourceSpanFactory.Span()));
}
