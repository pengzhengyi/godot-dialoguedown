using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class TagExtensionsTests
{
    [Fact]
    public void SemanticKey_IsNameAndValue()
    {
        var tag = new CustomTag("mood", "happy", new SourceSpan(3, 4));

        Assert.Equal(("mood", "happy"), tag.SemanticKey());
    }

    [Fact]
    public void SemanticKey_IgnoresTheSpan()
    {
        var here = new CustomTag("mood", "happy", new SourceSpan(0, 1));
        var elsewhere = new CustomTag("mood", "happy", new SourceSpan(99, 5));

        Assert.Equal(here.SemanticKey(), elsewhere.SemanticKey());
    }

    [Fact]
    public void SemanticKey_TreatsADifferentValueAsDistinct()
    {
        var happy = new CustomTag("mood", "happy", new SourceSpan(0, 1));
        var sad = new CustomTag("mood", "sad", new SourceSpan(0, 1));

        Assert.NotEqual(happy.SemanticKey(), sad.SemanticKey());
    }

    [Fact]
    public void SemanticKey_HandlesANullValue()
    {
        var tag = new CustomTag("happy", null, new SourceSpan(0, 1));

        Assert.Equal(("happy", (string?)null), tag.SemanticKey());
    }
}
