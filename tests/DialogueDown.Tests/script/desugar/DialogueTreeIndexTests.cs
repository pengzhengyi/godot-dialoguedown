using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DialogueTreeIndexTests
{
    [Fact]
    public void OfType_ReturnsNodesOfAConcreteType()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        Assert.Single(index.OfType<Jump>());
        Assert.Single(index.OfType<SceneHeading>());
        Assert.Equal(2, index.OfType<Line>().Count()); // the spoken line and the choice's line
    }

    [Fact]
    public void OfType_ReturnsNodesOfABaseType()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        // Speaker is abstract; the query still finds the concrete reference under it.
        var speaker = Assert.Single(index.OfType<Speaker>());
        Assert.IsType<SpeakerNameReference>(speaker);
    }

    [Fact]
    public void OfType_KeepsDocumentOrder()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        var text = index.OfType<Text>().Select(t => t.Content).ToArray();
        Assert.Equal(["Scene", "hi", "go", "pick"], text);
    }

    [Fact]
    public void OfType_IncludesEveryNodeUnderScriptNode()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        // "Scene" + heading + spoken line + speaker + "hi" + jump + "go" + choices
        // + choice + choice's line + "pick" = 11 nodes.
        Assert.Equal(11, index.OfType<ScriptNode>().Count());
    }

    [Fact]
    public void OfType_UnindexedType_IsEmpty()
    {
        var index = DialogueTreeIndex.Build(SampleDocument());

        Assert.Empty(index.OfType<Query>());
    }

    [Fact]
    public void Build_EmptyDocument_HasNoNodes()
    {
        var index = DialogueTreeIndex.Build(new DesugaredScriptDocument(new ScriptDocument([])));

        Assert.Empty(index.OfType<ScriptNode>());
    }

    [Fact]
    public void Build_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DialogueTreeIndex.Build(null!));

    // ## Scene
    // Alice: hi => [go](#play)
    // - pick
    private static DesugaredScriptDocument SampleDocument()
    {
        var heading = SceneHeading("Scene", 1);
        var spoken = new Line(
            SpeakerNameReference("Alice"),
            [Text("hi"), Jump("#play", Text("go"))],
            SourceSpanFactory.Span());
        var choices = new Choices(
            false, [Choice(Line(Text("pick")))], SourceSpanFactory.Span());
        return new DesugaredScriptDocument(new ScriptDocument([heading, spoken, choices]));
    }
}
