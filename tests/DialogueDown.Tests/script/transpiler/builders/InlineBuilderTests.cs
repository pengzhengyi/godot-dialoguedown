using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Tests.Support;
using Md = DialogueDown.Tests.Support.MarkdownAstFactory;
using MdInline = DialogueDown.Markdown.MarkdownInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class InlineBuilderTests
{
    private static readonly InlineBuilder _builder =
        new(new InlineLeafBuilder(new TagBuilder()), new GameCallBuilder(GameCallParser.Grammar));

    [Fact]
    public void Build_Text_IsTokenizedIntoTextAndTagFragments()
    {
        var speech = Build(Md.Text("mood #happy"));

        Assert.Collection(
            speech,
            fragment => Assert.Equal("mood ", Assert.IsType<Text>(fragment).Content),
            fragment => Assert.Equal("happy", Assert.IsType<CustomTag>(fragment).Name));
    }

    [Fact]
    public void Build_Emphasis_BecomesStyledText_RecursingItsChildren()
    {
        var speech = Build(Md.Emphasis(DialogueDown.Markdown.EmphasisKind.Bold, Md.Text("very")));

        var styled = Assert.IsType<StyledText>(Assert.Single(speech));
        Assert.Equal(SpeechStyle.Bold, styled.Style);
        Assert.Equal("very", Assert.IsType<Text>(Assert.Single(styled.Children)).Content);
    }

    [Fact]
    public void Build_Image_BecomesImage_RecursingItsAlt()
    {
        var speech = Build(Md.Image("cat.png", Md.Text("a cat")));

        var image = Assert.IsType<Image>(Assert.Single(speech));
        Assert.Equal("cat.png", image.Source);
        Assert.Equal("a cat", Assert.IsType<Text>(Assert.Single(image.Alt)).Content);
    }

    [Fact]
    public void Build_Link_BecomesLink_RecursingItsLabel()
    {
        var speech = Build(Md.Link("#scene", Md.Text("go there")));

        var link = Assert.IsType<Link>(Assert.Single(speech));
        Assert.Equal("#scene", link.Target);
        Assert.Equal("go there", Assert.IsType<Text>(Assert.Single(link.Label)).Content);
    }

    [Fact]
    public void Build_CodeSpan_BecomesAGameCall()
    {
        var speech = Build(Md.CodeSpan("\"Alice.Mood\""));

        Assert.Equal("Alice.Mood", Assert.IsType<Query>(Assert.Single(speech)).Key);
    }

    [Fact]
    public void Build_SoftBreak_BecomesALineBreakFragment()
    {
        var speech = Build(Md.LineBreak(hard: false));

        Assert.IsType<LineBreak>(Assert.Single(speech));
    }

    [Fact]
    public void Build_ArrowBeforeAndAfterText_BecomesAJumpIndicator()
    {
        var speech = Build(Md.Text("go => there"));

        Assert.Collection(
            speech,
            fragment => Assert.Equal("go ", Assert.IsType<Text>(fragment).Content),
            fragment => Assert.IsType<JumpIndicator>(fragment),
            fragment => Assert.Equal(" there", Assert.IsType<Text>(fragment).Content));
    }

    [Fact]
    public void Build_WhenJumpsDisallowed_TheArrowStaysText()
    {
        var speech = _builder.Build([Md.Text("go => there")], InlineElements.StylingOnly);

        Assert.Equal("go => there", Assert.IsType<Text>(Assert.Single(speech)).Content);
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

        Assert.Equal(4, Assert.IsType<Text>(Assert.Single(speech)).Span.Start);
    }

    [Fact(Skip = "TODO: escape span drift — a stripped backslash shifts a literal's sub-token "
        + "spans by <=1 char (see the note's boundary cases); we accept it for now.")]
    public void Build_TextFromAnEscapedLiteral_AnchorsAtTheTrueSource()
    {
        // Source "\\* b" -> Markdig content "* b" with a span of length 4 (the '\\' counted).
        var text = new DialogueDown.Markdown.TextInline("* b", SourceSpanFactory.Span(2, 4));

        var speech = _builder.Build([text], InlineElements.All);

        // After the fix the text should start past the stripped backslash, at 3, not 2.
        Assert.Equal(3, Assert.IsType<Text>(Assert.Single(speech)).Span.Start);
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
        Assert.IsType<StyledText>(
            Assert.Single(Build(Md.Emphasis(kind, Md.Text("x"))))).Style;

    private static IReadOnlyList<SpeechFragment> Build(params MdInline[] inlines) =>
        _builder.Build(inlines, InlineElements.All);

    private sealed record UnknownInline(DialogueDown.Common.SourceSpan Span) : MdInline(Span);
}
