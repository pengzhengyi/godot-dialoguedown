using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;
using MdEmphasisKind = DialogueDown.Markdown.EmphasisKind;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class InlineBuilderTests
{
    private static readonly InlineBuilder _builder = TranspilerBuilderFactory.InlineBuilder();

    [Fact]
    public void Build_Text_IsTokenizedIntoTextAndTagFragments()
    {
        var speech = _builder.Build([Md.Text("mood #happy")]);

        Assert.Collection(
            speech,
            fragment => AssertText(fragment, "mood "),
            fragment => AssertCustomTag(fragment, "happy"));
    }

    [Fact]
    public void Build_Emphasis_BecomesStyledText_RecursingItsChildren()
    {
        var speech = _builder.Build([Md.Emphasis(MdEmphasisKind.Bold, Md.Text("very"))]);

        var styled = AssertStyledText(Assert.Single(speech), SpeechStyle.Bold);
        AssertText(Assert.Single(styled.Children), "very");
    }

    [Fact]
    public void Build_Image_BecomesImage_RecursingItsAlt()
    {
        var speech = _builder.Build([Md.Image("cat.png", Md.Text("a cat"))]);

        var image = AssertImage(Assert.Single(speech), "cat.png");
        AssertText(Assert.Single(image.Alt), "a cat");
    }

    [Fact]
    public void Build_Link_BecomesLink_RecursingItsLabel()
    {
        var speech = _builder.Build([Md.Link("#scene", Md.Text("go there"))]);

        var link = AssertLink(Assert.Single(speech), "#scene");
        AssertText(Assert.Single(link.Label), "go there");
    }

    [Fact]
    public void Build_CodeSpan_BecomesAGameCall()
    {
        var speech = _builder.Build([Md.CodeSpan("\"Alice.Mood\"")]);

        AssertQuery(Assert.Single(speech), "Alice.Mood");
    }

    [Fact]
    public void Build_SoftBreak_BecomesALineBreakFragment()
    {
        var speech = _builder.Build([Md.LineBreak(hard: false)]);

        AssertLineBreak(Assert.Single(speech));
    }

    [Fact]
    public void Build_ArrowBeforeAndAfterText_BecomesAJumpIndicator()
    {
        var speech = _builder.Build([Md.Text("go => there")]);

        Assert.Collection(
            speech,
            fragment => AssertText(fragment, "go "),
            fragment => AssertJumpIndicator(fragment),
            fragment => AssertText(fragment, " there"));
    }

    [Fact]
    public void Build_LabelTagIsKept_ButJumpStaysText()
    {
        // A label admits text and tags, but not jumps: '=>' stays literal there.
        var speech = _builder.Build([Md.Link("#x", Md.Text("go => #here"))]);

        var link = AssertLink(Assert.Single(speech), "#x");
        Assert.Collection(
            link.Label,
            fragment => AssertText(fragment, "go => "),
            fragment => AssertCustomTag(fragment, "here"));
    }

    [Fact]
    public void Build_CodeSpanInLabel_IsRestoredToLiteralText()
    {
        // A code span carries no game call inside a label, so it comes back as text.
        var speech = _builder.Build([Md.Link("#x", Md.CodeSpan("q"))]);

        var link = AssertLink(Assert.Single(speech), "#x");
        AssertText(Assert.Single(link.Label), "`q`");
    }

    [Fact]
    public void Build_NestedImageInLabel_IsRestoredToLiteralText()
    {
        var speech = _builder.Build([Md.Link("#x", Md.Image("img.png", Md.Text("alt")))]);

        var link = AssertLink(Assert.Single(speech), "#x");
        AssertText(Assert.Single(link.Label), "![alt](img.png)");
    }

    [Fact]
    public void Build_StyledLabel_IsKept()
    {
        // Styling is admitted inside a label.
        var speech = _builder.Build(
            [Md.Link("#x", Md.Emphasis(MdEmphasisKind.Bold, Md.Text("loud")))]);

        var link = AssertLink(Assert.Single(speech), "#x");
        var styled = AssertStyledText(Assert.Single(link.Label), SpeechStyle.Bold);
        AssertText(Assert.Single(styled.Children), "loud");
    }

    [Fact]
    public void Build_AllStyles_MapToTheirDialogueStyle()
    {
        Assert.Equal(SpeechStyle.Italic, StyleOf(MdEmphasisKind.Italic));
        Assert.Equal(SpeechStyle.Bold, StyleOf(MdEmphasisKind.Bold));
        Assert.Equal(SpeechStyle.Strikethrough, StyleOf(MdEmphasisKind.Strikethrough));
    }

    [Fact]
    public void Build_AnchorsFragmentSpansAtTheInlineSource()
    {
        // A text inline sitting at source offset 4.
        var text = new DialogueDown.Markdown.TextInline("hi", SourceSpanFactory.Span(4, 2));

        var speech = _builder.Build([text]);

        Assert.Equal(4, AssertText(Assert.Single(speech), "hi").Span.Start);
    }

    [Fact]
    public void Build_TextFromAnEscapedLiteral_AnchorsAtTheContentSpan()
    {
        // Source "\* b": Markdig yields content "* b" with a raw span [2,6) that counts
        // the backslash, and a ContentSpan [3,6) that starts past it. The built text must
        // anchor at the content, not the raw span.
        var text = new DialogueDown.Markdown.TextInline(
            "* b", SourceSpanFactory.Span(2, 4), SourceSpanFactory.Span(3, 3));

        var speech = _builder.Build([text]);

        Assert.Equal(3, AssertText(Assert.Single(speech), "* b").Span.Start);
    }

    [Fact]
    public void Build_UnknownEmphasisKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build([Md.Emphasis((MdEmphasisKind)99, Md.Text("x"))]));

    [Fact]
    public void Build_UnhandledInlineKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build([new UnknownMarkdownInline(SourceSpanFactory.Span())]));

    [Fact]
    public void Build_WithStrictPolicy_RejectsACodeSpanInALabel()
    {
        var builder = TranspilerBuilderFactory.InlineBuilder(new RejectingInlinePolicy());

        var error = Assert.Throws<DialogueSyntaxError>(
            () => builder.Build([Md.Link("#x", Md.CodeSpan("q"))]));

        Assert.Contains("not allowed inside a label", error.Message);
    }

    private static SpeechStyle StyleOf(MdEmphasisKind kind) =>
        AssertStyledText(Assert.Single(_builder.Build([Md.Emphasis(kind, Md.Text("x"))]))).Style;
}
