using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using MarkdigMarkdownParser = DialogueDown.Markdown.MarkdigMarkdownParser;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class ScriptTranspilerTests
{
    private readonly ScriptTranspiler _transpiler = TranspilerBuilderFactory.ScriptTranspiler();

    [Fact]
    public void Transpile_WrapsTheBuiltBlocksInAScriptDocument()
    {
        var source =
            """
            # The Cave

            Alice: Hi
            """;
        var document = Document(Heading(1, Text("The Cave")), TextParagraph("Alice: Hi"));

        var script = _transpiler.Transpile(document, source);

        Assert.Equal(2, script.Body.Count);
        AssertSceneHeading(script.Body[0], "The Cave", 1);
        var line = AssertLine(script.Body[1]);
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hi");
    }

    [Fact]
    public void Transpile_EmptyDocument_HasEmptyBody() =>
        Assert.Empty(_transpiler.Transpile(Document(), string.Empty).Body);

    [Fact]
    public void Transpile_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _transpiler.Transpile(null!, "source"));

    [Fact]
    public void Transpile_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _transpiler.Transpile(Document(), null!));

    [Fact]
    public void Transpile_AParsedScript_ProducesTheDialogueTree()
    {
        var source =
            """
            # The Cave

            Alice: Hello there.

            - Go left
            - Go right
            """;
        var document = new MarkdigMarkdownParser().Parse(source);

        var script = _transpiler.Transpile(document, source);

        Assert.Equal(3, script.Body.Count);
        AssertSceneHeading(script.Body[0], "The Cave", 1);
        var line = AssertLine(script.Body[1]);
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
        var choices = AssertChoices(script.Body[2], isOrdered: false);
        Assert.Equal(2, choices.Options.Count);
        AssertSpeechText(AssertChoiceLine(choices.Options[0]), "Go left");
        AssertSpeechText(AssertChoiceLine(choices.Options[1]), "Go right");
    }
}
