using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class InlineFragmentExtensionsTests
{
    [Fact]
    public void IsBlank_TextOfOnlyWhitespace_IsTrue()
    {
        Assert.True(Text("   ").IsBlank());
    }

    [Fact]
    public void IsBlank_TextWithVisibleContent_IsFalse()
    {
        Assert.False(Text("go").IsBlank());
    }

    [Fact]
    public void IsBlank_LineBreak_IsFalse()
    {
        // A soft break ends a single-line construct; it is not padding.
        Assert.False(LineBreak().IsBlank());
    }

    [Fact]
    public void IsBlank_NonTextFragment_IsFalse()
    {
        Assert.False(new Link("#play", [Text("go")], new SourceSpan(0, 5)).IsBlank());
    }
}
