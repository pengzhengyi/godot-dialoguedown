using DialogueDown.Common.Errors;
using DialogueDown.Script.Semantics.Errors;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Semantics.Errors;

public sealed class DialogueSemanticErrorTests
{
    [Fact]
    public void Constructor_MessageAndSpan_AreExposed()
    {
        var span = SourceSpanFactory.Span(3, 4);

        var error = new DialogueSemanticError("speaker @a is never named", span);

        Assert.Equal("speaker @a is never named", error.Message);
        Assert.Equal(span, error.Span);
    }

    [Fact]
    public void DialogueSemanticError_SitsInTheSemanticBranchOfTheHierarchy()
    {
        var error = new DialogueSemanticError("x", SourceSpanFactory.Span());

        Assert.IsAssignableFrom<SemanticError>(error);
        Assert.IsAssignableFrom<ScriptCompilationException>(error);
        Assert.IsAssignableFrom<DialogueDownException>(error);
        Assert.IsAssignableFrom<Exception>(error);
    }
}
