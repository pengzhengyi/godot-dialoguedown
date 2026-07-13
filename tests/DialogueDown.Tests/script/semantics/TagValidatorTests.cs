using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Semantics.Errors;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class TagValidatorTests
{
    [Fact]
    public void Validate_KnownReservedTag_Passes() =>
        TagValidator.Validate([Reserved("default")]); // no throw

    [Fact]
    public void Validate_NoTags_Passes() =>
        TagValidator.Validate([]); // no throw

    [Fact]
    public void Validate_UnknownReservedTag_Throws()
    {
        var error = Assert.Throws<DialogueSemanticError>(
            () => TagValidator.Validate([Reserved("bogus")]));

        Assert.Contains("##bogus", error.Message);
    }

    [Fact]
    public void Validate_ReportsTheOffendingTagSpan()
    {
        var span = new SourceSpan(4, 6);

        var error = Assert.Throws<DialogueSemanticError>(
            () => TagValidator.Validate([new ReservedTag("bogus", null, span)]));

        Assert.Equal(span, error.Span);
    }

    [Fact]
    public void Validate_StopsAtTheFirstUnknownTag()
    {
        var error = Assert.Throws<DialogueSemanticError>(
            () => TagValidator.Validate([Reserved("first-bad"), Reserved("second-bad")]));

        Assert.Contains("##first-bad", error.Message);
    }

    private static ReservedTag Reserved(string name) => new(name, null, SourceSpanFactory.Span());
}
