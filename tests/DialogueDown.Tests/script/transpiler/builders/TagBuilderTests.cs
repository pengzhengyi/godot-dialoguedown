using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class TagBuilderTests
{
    private static readonly TagBuilder _builder = new();

    [Fact]
    public void CustomData_BecomesACustomTag_WithTheGivenSpan()
    {
        var span = SourceSpanFactory.Span(2, 5);

        var tag = _builder.Build(new TagData(IsReserved: false, "main", null), span);

        var custom = AssertCustomTag(tag, "main");
        Assert.Equal(span, custom.Span);
    }

    [Fact]
    public void ReservedData_BecomesAReservedTag() =>
        AssertReservedTag(
            _builder.Build(new TagData(IsReserved: true, "default", null), SourceSpanFactory.Span()),
            "default");

    [Fact]
    public void GroupValue_IsCarried() =>
        AssertCustomTag(
            _builder.Build(new TagData(IsReserved: false, "mood", "happy"), SourceSpanFactory.Span()),
            "mood",
            "happy");
}
