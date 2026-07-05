using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Ast;

public sealed class SpeakerIdReferenceTests
{
    [Fact]
    public void IdReference_ExposesIdAndSpan_AndIsASpeakerReference()
    {
        var span = SourceSpanFactory.Span();

        var reference = new SpeakerIdReference("A", span);

        var asserted = AssertSpeakerIdReference(reference, "A");
        Assert.Equal(span, asserted.Span);
        Assert.IsAssignableFrom<SpeakerReference>(asserted);
    }
}
