using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Errors;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;
using MdEmphasisKind = DialogueDown.Markdown.EmphasisKind;
using MdInline = DialogueDown.Markdown.MarkdownInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class RejectingInlinePolicyTests
{
    private static readonly RejectingInlinePolicy _policy = new();

    [Fact]
    public void Supports_TextAndStyling_ButNotFunctionalElements()
    {
        Assert.True(_policy.Supports(Md.Text("hi")));
        Assert.True(_policy.Supports(Md.Emphasis(MdEmphasisKind.Bold, Md.Text("hi"))));
        Assert.False(_policy.Supports(Md.CodeSpan("q")));
    }

    [Fact]
    public void SupportsJumps_IsFalse() => Assert.False(_policy.SupportsJumps);

    [Theory]
    [InlineData("code")]
    [InlineData("image")]
    [InlineData("link")]
    [InlineData("break")]
    public void Resolve_AnyFunctionalElement_Throws(string kind)
    {
        var inline = kind switch
        {
            "code" => (MdInline)Md.CodeSpan("q"),
            "image" => Md.Image("i.png", Md.Text("a")),
            "link" => Md.Link("#x", Md.Text("l")),
            _ => Md.LineBreak(),
        };

        var error = Assert.Throws<DialogueSyntaxError>(() => _policy.Resolve(inline));
        Assert.Contains("not allowed inside a label", error.Message);
    }

    [Fact]
    public void Resolve_AnUnnamedElement_StillThrows()
    {
        // An element with no specific description falls back to a generic message.
        var error = Assert.Throws<DialogueSyntaxError>(
            () => _policy.Resolve(new UnknownInline(Md.Span())));

        Assert.Contains("not allowed inside a label", error.Message);
    }

    private sealed record UnknownInline(DialogueDown.Common.SourceSpan Span) : MdInline(Span);
}
