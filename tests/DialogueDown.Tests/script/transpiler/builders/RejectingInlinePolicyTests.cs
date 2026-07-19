using DialogueDown.Diagnostics;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.InlinePolicyAssert;
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
    public void Resolve_AnyFunctionalElement_ReportsAndDropsIt(string kind)
    {
        var inline = kind switch
        {
            "code" => (MdInline)Md.CodeSpan("q"),
            "image" => Md.Image("i.png", Md.Text("a")),
            "link" => Md.Link("#x", Md.Text("l")),
            _ => Md.LineBreak(),
        };

        Assert.Empty(Resolve(_policy, inline, out var diagnostics));
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DisallowedLabelElement);
    }

    [Fact]
    public void Resolve_AnUnnamedElement_StillReports()
    {
        // An element with no specific description falls back to a generic message.
        Assert.Empty(Resolve(_policy, new UnknownMarkdownInline(Md.Span()), out var diagnostics));
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.DisallowedLabelElement);
    }
}
