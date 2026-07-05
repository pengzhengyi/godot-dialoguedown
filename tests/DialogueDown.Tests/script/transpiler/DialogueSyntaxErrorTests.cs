using DialogueDown.Common.Errors;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class DialogueSyntaxErrorTests
{
    [Fact]
    public void Constructor_MessageAndSpan_AreExposed()
    {
        var span = SourceSpanFactory.Span(3, 4);

        var error = new DialogueSyntaxError("a code span is not a game call", span);

        Assert.Equal("a code span is not a game call", error.Message);
        Assert.Equal(span, error.Span);
    }

    [Fact]
    public void DialogueSyntaxError_SitsInTheSyntaxBranchOfTheHierarchy()
    {
        var error = new DialogueSyntaxError("x", SourceSpanFactory.Span());

        Assert.IsAssignableFrom<SyntaxError>(error);
        Assert.IsAssignableFrom<ScriptCompilationException>(error);
        Assert.IsAssignableFrom<DialogueDownException>(error);
        Assert.IsAssignableFrom<Exception>(error);
    }
}
