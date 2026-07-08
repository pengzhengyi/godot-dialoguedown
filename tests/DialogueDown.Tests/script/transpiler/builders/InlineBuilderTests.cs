using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;
using MdInline = DialogueDown.Markdown.MarkdownInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class InlineBuilderTests
{
    private static readonly InlineBuilder _builder =
        TranspilerBuilderFactory.InlineBuilder();

    [Fact]
    public void Build_Text_IsTokenizedIntoTextAndTagFragments()
    {
        var speech = Build(Md.Text("mood #happy"));

        Assert.Collection(
            speech,
            fragment => AssertText(fragment, "mood "),
            fragment => AssertCustomTag(fragment, "happy"));
    }

    [Fact]
    public void Build_Emphasis_BecomesStyledText_RecursingItsChildren()
    {
        var speech = Build(Md.Emphasis(DialogueDown.Markdown.EmphasisKind.Bold, Md.Text("very")));

        var styled = AssertStyledText(Assert.Single(speech), SpeechStyle.Bold);
        AssertText(Assert.Single(styled.Children), "very");
    }

    [Fact]
    public void Build_Image_BecomesImage_RecursingItsAlt()
    {
        var speech = Build(Md.Image("cat.png", Md.Text("a cat")));

        var image = AssertImage(Assert.Single(speech), "cat.png");
        AssertText(Assert.Single(image.Alt), "a cat");
    }

    [Fact]
    public void Build_Link_BecomesLink_RecursingItsLabel()
    {
        var speech = Build(Md.Link("#scene", Md.Text("go there")));

        var link = AssertLink(Assert.Single(speech), "#scene");
        AssertText(Assert.Single(link.Label), "go there");
    }

    [Fact]
    public void Build_CodeSpan_BecomesAGameCall()
    {
        var speech = Build(Md.CodeSpan("\"Alice.Mood\""));

        AssertQuery(Assert.Single(speech), "Alice.Mood");
    }

    [Fact]
    public void Build_SoftBreak_BecomesALineBreakFragment()
    {
        var speech = Build(Md.LineBreak(hard: false));

        AssertLineBreak(Assert.Single(speech));
    }

    [Fact]
    public void Build_ArrowBeforeAndAfterText_BecomesAJumpIndicator()
    {
        var speech = Build(Md.Text("go => there"));

        Assert.Collection(
            speech,
            fragment => AssertText(fragment, "go "),
            fragment => AssertJumpIndicator(fragment),
            fragment => AssertText(fragment, " there"));
    }

    [Fact]
    public void Build_WhenJumpsDisallowed_TheArrowStaysText()
    {
        var speech = _builder.Build([Md.Text("go => there")], InlineElements.StylingOnly);

        AssertText(Assert.Single(speech), "go => there");
    }

    [Fact]
    public void Build_AllStyles_MapToTheirDialogueStyle()
    {
        Assert.Equal(SpeechStyle.Italic, StyleOf(DialogueDown.Markdown.EmphasisKind.Italic));
        Assert.Equal(SpeechStyle.Bold, StyleOf(DialogueDown.Markdown.EmphasisKind.Bold));
        Assert.Equal(SpeechStyle.Strikethrough, StyleOf(DialogueDown.Markdown.EmphasisKind.Strikethrough));
    }

    [Fact]
    public void Build_AnchorsFragmentSpansAtTheInlineSource()
    {
        // A text inline sitting at source offset 4.
        var text = new DialogueDown.Markdown.TextInline("hi", SourceSpanFactory.Span(4, 2));

        var speech = _builder.Build([text], InlineElements.All);

        Assert.Equal(4, AssertText(Assert.Single(speech), "hi").Span.Start);
    }

    [Fact(Skip = "TODO: escape span drift — a stripped backslash shifts a literal's sub-token "
        + "spans by <=1 char (see the note's boundary cases); we accept it for now.")]
    public void Build_TextFromAnEscapedLiteral_AnchorsAtTheTrueSource()
    {
        // Source "\\* b" -> Markdig content "* b" with a span of length 4 (the '\\' counted).
        var text = new DialogueDown.Markdown.TextInline("* b", SourceSpanFactory.Span(2, 4));

        var speech = _builder.Build([text], InlineElements.All);

        // After the fix the text should start past the stripped backslash, at 3, not 2.
        Assert.Equal(3, AssertText(Assert.Single(speech), "* b").Span.Start);
    }

    [Fact]
    public void Build_UnknownEmphasisKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Build(Md.Emphasis((DialogueDown.Markdown.EmphasisKind)99, Md.Text("x"))));

    [Fact]
    public void Build_UnhandledInlineKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Build(new UnknownInline(SourceSpanFactory.Span())));

    private static SpeechStyle StyleOf(DialogueDown.Markdown.EmphasisKind kind) =>
        AssertStyledText(Assert.Single(Build(Md.Emphasis(kind, Md.Text("x"))))).Style;

    private static IReadOnlyList<SpeechFragment> Build(params MdInline[] inlines) =>
        _builder.Build(inlines, InlineElements.All);

    private sealed record UnknownInline(DialogueDown.Common.SourceSpan Span) : MdInline(Span);
}
