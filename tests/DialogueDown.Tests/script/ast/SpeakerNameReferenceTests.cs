using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Script.Ast;

public sealed class SpeakerNameReferenceTests
{
    [Fact]
    public void NameReference_ExposesNameAndSpan_AndIsASpeaker()
    {
        var span = SourceSpanFactory.Span();

        var reference = new SpeakerNameReference("Alice", span);

        var asserted = AssertSpeakerNameReference(reference, "Alice");
        Assert.Equal(span, asserted.Span);
        Assert.IsAssignableFrom<Speaker>(asserted);
        Assert.IsAssignableFrom<ScriptNode>(asserted);
    }
}
