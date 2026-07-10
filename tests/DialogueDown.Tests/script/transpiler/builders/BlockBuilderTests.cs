using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class BlockBuilderTests
{
    private readonly BlockBuilder _builder = TranspilerBuilderFactory.BlockBuilder();

    [Fact]
    public void EmptyDocument_HasEmptyBody() =>
        Assert.Empty(_builder.Build([]));

    [Fact]
    public void Heading_BecomesAFlatSceneHeading()
    {
        var heading = Heading(2, Text("The Cave"));

        var body = _builder.Build([heading]);

        var scene = AssertSceneHeading(Assert.Single(body), "The Cave", 2);
        Assert.Equal(heading.Span, scene.Span);
    }

    [Fact]
    public void Heading_ArrowIsPlainText_NotAJump()
    {
        // A heading becomes a scene, which is itself a jump target, so a `=>` inside a
        // heading is not a jump — it reads as plain text.
        var scene = Assert.IsType<SceneHeading>(
            Assert.Single(_builder.Build([Heading(1, Text("=> "), Link("#x", Text("there")))])));

        Assert.Equal(2, scene.Title.Count);
        AssertText(scene.Title[0], "=> ");
        AssertLink(scene.Title[1], "#x");
        Assert.DoesNotContain(scene.Title, fragment => fragment is JumpIndicator);
    }

    [Fact]
    public void Heading_StillRecognizesTagsAndGameCalls()
    {
        var scene = Assert.IsType<SceneHeading>(
            Assert.Single(_builder.Build([Heading(1, Text("Chapter #act1"), CodeSpan("\"n\""))])));

        AssertText(scene.Title[0], "Chapter ");
        AssertCustomTag(scene.Title[1], "act1");
        AssertQuery(scene.Title[2], "n");
    }

    [Fact]
    public void Paragraph_BecomesALine()
    {
        var body = _builder.Build([TextParagraph("Alice: Hello there.")]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void Paragraph_HardBreak_SplitsIntoOneLinePerSlice()
    {
        var body = _builder.Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: true), Text("Bob: Yo"))]);

        Assert.Equal(2, body.Count);
        var first = AssertLine(body[0]);
        AssertSpeakerNameReference(first.Speaker!, "Alice");
        AssertSpeechText(first, "Hi");
        var second = AssertLine(body[1]);
        AssertSpeakerNameReference(second.Speaker!, "Bob");
        AssertSpeechText(second, "Yo");
    }

    [Fact]
    public void Paragraph_SoftBreak_StaysWithinOneLine()
    {
        var body = _builder.Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: false), Text("there"))]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(3, line.Speech.Count);
        AssertText(line.Speech[0], "Hi");
        AssertLineBreak(line.Speech[1]);
        AssertText(line.Speech[2], "there");
    }

    [Fact]
    public void Paragraph_EmptyGroupsAroundHardBreaks_AreDropped()
    {
        var body = _builder.Build(
        [
            Paragraph(
                LineBreak(hard: true),                        // leading break -> empty group
                Text("Alice: Hi"),
                LineBreak(hard: true),
                LineBreak(hard: true),                        // doubled break -> empty group
                Text("Bob: Yo"),
                LineBreak(hard: true)),                       // trailing break -> empty group
        ]);

        Assert.Equal(2, body.Count);
        AssertSpeakerNameReference(AssertLine(body[0]).Speaker!, "Alice");
        AssertSpeakerNameReference(AssertLine(body[1]).Speaker!, "Bob");
    }

    [Fact]
    public void Paragraph_OnlyHardBreaks_EmitsNoLine() =>
        Assert.Empty(_builder.Build([Paragraph(LineBreak(hard: true))]));

    [Fact]
    public void List_BecomesChoices_OneChoicePerItem()
    {
        var body = _builder.Build(
        [
            ListBlock(
                ordered: false,
                ListItem(TextParagraph("Go north")),
                ListItem(TextParagraph("Go south"))),
        ]);

        var choices = AssertChoices(Assert.Single(body), isOrdered: false);
        Assert.Equal(2, choices.Options.Count);
        AssertSpeechText(AssertChoiceLine(choices.Options[0]), "Go north");
        AssertSpeechText(AssertChoiceLine(choices.Options[1]), "Go south");
    }

    [Fact]
    public void OrderedList_KeepsIsOrdered() =>
        AssertChoices(
            Assert.Single(_builder.Build([ListBlock(ordered: true, ListItem(TextParagraph("First")))])),
            isOrdered: true);

    [Fact]
    public void ChoiceItem_MayCarryASpeaker()
    {
        var body = _builder.Build([ListBlock(ordered: false, ListItem(TextParagraph("Alice: Hi")))]);

        var choices = AssertChoices(Assert.Single(body), isOrdered: false);
        var line = AssertChoiceLine(Assert.Single(choices.Options));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hi");
    }

    [Fact]
    public void ChoiceItem_WithMultipleBlocks_KeepsThemAll()
    {
        var body = _builder.Build(
        [
            ListBlock(
                ordered: false,
                ListItem(TextParagraph("Line one"), TextParagraph("Line two"))),
        ]);

        var choice = Assert.Single(AssertChoices(Assert.Single(body), isOrdered: false).Options);
        Assert.Equal(2, choice.Body.Count);
        AssertSpeechText(AssertLine(choice.Body[0]), "Line one");
        AssertSpeechText(AssertLine(choice.Body[1]), "Line two");
    }

    [Fact]
    public void NestedList_BecomesNestedChoices()
    {
        var innerList = ListBlock(ordered: false, ListItem(TextParagraph("Inner")));
        var outerList = ListBlock(
            ordered: false,
            ListItem(TextParagraph("Pick one:"), innerList));

        var body = _builder.Build([outerList]);

        // The outer list is one Choices with a single option.
        var choice = Assert.Single(AssertChoices(Assert.Single(body), isOrdered: false).Options);

        // That option holds its line, then the nested list as a nested Choices.
        Assert.Equal(2, choice.Body.Count);
        AssertSpeechText(AssertLine(choice.Body[0]), "Pick one:");
        var inner = AssertChoices(choice.Body[1], isOrdered: false);
        AssertSpeechText(AssertChoiceLine(Assert.Single(inner.Options)), "Inner");
    }

    [Fact]
    public void Choices_CarryListAndItemSpans()
    {
        var item = ListItem(TextParagraph("Go north"));
        var list = ListBlock(ordered: false, item);

        var choices = AssertChoices(Assert.Single(_builder.Build([list])), isOrdered: false);

        Assert.Equal(list.Span, choices.Span);
        Assert.Equal(item.Span, Assert.Single(choices.Options).Span);
    }

    [Fact]
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build([new UnknownMarkdownBlock(Span())]));
}
