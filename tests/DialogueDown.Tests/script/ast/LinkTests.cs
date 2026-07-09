using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class LinkTests
{
    [Fact]
    public void Constructor_ExposesTargetLabelAndSpan_AndIsAInlineFragment()
    {
        var span = SourceSpanFactory.Span();
        var label = new InlineFragment[] { Text("the old well"), CustomTag("ominous") };

        var link = new Link("#well", label, span);

        Assert.Equal("#well", link.Target);
        Assert.Equal(label, link.Label);
        Assert.Equal(span, link.Span);
        Assert.IsAssignableFrom<InlineFragment>(link);
        Assert.IsAssignableFrom<ScriptNode>(link);
    }

    [Fact]
    public void Constructor_AllowsEmptyLabel() =>
        Assert.Empty(new Link("#well", [], SourceSpanFactory.Span()).Label);

    [Fact]
    public void Constructor_AllowsEmptyTarget_LeavingItUnresolved() =>
        // An empty target is a valid syntactic form; the semantic analyzer judges it.
        Assert.Equal(string.Empty, new Link(string.Empty, [], SourceSpanFactory.Span()).Target);

    [Fact]
    public void Constructor_NullTarget_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new Link(null!, [], SourceSpanFactory.Span()));
}
