using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ReservedTagTests
{
    [Fact]
    public void Constructor_PlainTag_ExposesNameNoValueAndSpan()
    {
        var span = SourceSpanFactory.Span();

        var tag = AssertReservedTag(new ReservedTag("default", Value: null, span), "default");

        Assert.Equal(span, tag.Span);
    }

    [Fact]
    public void Constructor_GroupTag_ExposesValue() =>
        AssertReservedTag(
            new ReservedTag("mode", "silent", SourceSpanFactory.Span()), "mode", "silent");
}
