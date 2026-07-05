using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Ast;

public sealed class CustomTagTests
{
    [Fact]
    public void Constructor_PlainTag_ExposesNameNoValueSpan_AndIsASpeechFragment()
    {
        var span = SourceSpanFactory.Span();

        var tag = AssertCustomTag(new CustomTag("main", Value: null, span), "main");

        Assert.Equal(span, tag.Span);
        Assert.IsAssignableFrom<SpeechFragment>(tag);
    }

    [Fact]
    public void Constructor_GroupTag_ExposesValue() =>
        AssertCustomTag(new CustomTag("mood", "happy", SourceSpanFactory.Span()), "mood", "happy");
}
