using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class StyledTextTests
{
    [Fact]
    public void Constructor_ExposesStyleChildrenAndSpan_AndIsASpeechFragment()
    {
        var span = SourceSpanFactory.Span();
        var children = new SpeechFragment[] { Text("very") };

        var styled = new StyledText(SpeechStyle.Bold, children, span);

        Assert.Equal(SpeechStyle.Bold, styled.Style);
        Assert.Equal(children, styled.Children);
        Assert.Equal(span, styled.Span);
        Assert.IsAssignableFrom<SpeechFragment>(styled);
        Assert.IsAssignableFrom<ScriptNode>(styled);
    }

    [Fact]
    public void Constructor_AcceptsEachStyle()
    {
        foreach (var style in new[] { SpeechStyle.Italic, SpeechStyle.Bold, SpeechStyle.Strikethrough })
        {
            Assert.Equal(style, new StyledText(style, [Text("x")], SourceSpanFactory.Span()).Style);
        }
    }

    [Fact]
    public void Constructor_NestsFragments_SoStylesCompose()
    {
        // Bold-italic is one style nested inside another.
        var inner = new StyledText(SpeechStyle.Italic, [Text("shiny")], SourceSpanFactory.Span());

        var outer = new StyledText(SpeechStyle.Bold, [inner], SourceSpanFactory.Span());

        Assert.Same(inner, Assert.Single(outer.Children));
    }

    [Fact]
    public void Constructor_EmptyChildren_Throws() =>
        // Markdig degrades empty emphasis (like ****) to plain text, so this never occurs.
        Assert.Throws<ArgumentException>(
            () => new StyledText(SpeechStyle.Bold, [], SourceSpanFactory.Span()));
}
