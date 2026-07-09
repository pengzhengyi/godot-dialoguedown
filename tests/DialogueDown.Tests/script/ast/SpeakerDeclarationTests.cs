using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class SpeakerDeclarationTests
{
    [Fact]
    public void Declaration_ExposesNameIdTagsAndSpan_AndIsASpeaker()
    {
        var span = SourceSpanFactory.Span();

        var declaration = new SpeakerDeclaration("Alice", "A", Tags(CustomTag("main")), span);

        var asserted = AssertSpeakerDeclaration(declaration, "Alice", "A", CustomTag("main"));
        Assert.Equal(span, asserted.Span);
        Assert.IsAssignableFrom<Speaker>(asserted);
    }

    [Fact]
    public void Declaration_DefaultsToNoIdAndNoTags()
    {
        var declaration = new SpeakerDeclaration("Alice", Id: null, [], SourceSpanFactory.Span());

        // No id and no tags assert as null and empty via the helper defaults.
        AssertSpeakerDeclaration(declaration, "Alice");
    }
}
