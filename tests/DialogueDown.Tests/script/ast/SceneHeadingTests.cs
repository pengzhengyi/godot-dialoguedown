using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class SceneHeadingTests
{
    [Fact]
    public void Constructor_ExposesTitleLevelAndSpan_AndIsABlock()
    {
        var span = SourceSpanFactory.Span();
        var title = new InlineFragment[] { Text("Greetings") };

        var heading = new SceneHeading(title, 2, span);

        Assert.Equal(title, heading.Title);
        Assert.Equal(2, heading.Level);
        Assert.Equal(span, heading.Span);
        Assert.IsAssignableFrom<ScriptBlock>(heading);
        Assert.IsAssignableFrom<ScriptNode>(heading);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Constructor_LevelOutsideOneToSix_Throws(int level) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SceneHeading([Text("t")], level, SourceSpanFactory.Span()));
}
