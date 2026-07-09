using DialogueDown.Markdown;
using static DialogueDown.Tests.Support.MarkdownAstAssert;
using Ast = DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class LinkInlineTests
{
    [Fact]
    public void Constructor_ExposesTargetLabelAndSpan_AndIsInline()
    {
        var span = Ast.Span();
        var label = new MarkdownInline[] { Ast.Text("Play tennis") };

        var inline = new LinkInline("#play-tennis", label, span);

        Assert.Equal("#play-tennis", inline.Target);
        Assert.Equal(label, inline.Label);
        Assert.Equal(span, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }

    [Fact]
    public void Label_CarriesInlineStructure() =>
        AssertSingleText(new LinkInline("#x", [Ast.Text("here")], Ast.Span()).Label, "here");
}
