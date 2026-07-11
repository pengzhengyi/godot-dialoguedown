using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization.Tests.Script;

public sealed class DialogueAstProjectionTests
{
    private readonly DialogueAstProjection _projection = new("Hi there");

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DialogueAstProjection(null!));
    }

    [Fact]
    public void Title_IsDialogueAst()
    {
        Assert.Equal("Dialogue AST", _projection.Title);
    }

    [Fact]
    public void Description_IsANonEmptyOneLiner()
    {
        Assert.False(string.IsNullOrWhiteSpace(_projection.Description));
        Assert.DoesNotContain('\n', _projection.Description);
    }

    [Fact]
    public void Describe_Document_LabelsDocumentWithTheWholeSource()
    {
        var description = _projection.Describe(new ScriptDocument([]));

        Assert.Equal("Document", description.Label);
        Assert.Equal("document", description.Category);
        Assert.Equal("Hi there", description.Source);
    }

    [Fact]
    public void Describe_UnsupportedNode_Throws()
    {
        Assert.Throws<ArgumentException>(() => _projection.Describe("not a node"));
    }

    [Fact]
    public void Describe_SlicesTheSourceFromTheSpan()
    {
        var description = _projection.Describe(new Text("Hi", new SourceSpan(0, 2)));

        Assert.Equal("Text", description.Label);
        Assert.Equal("text", description.Category);
        Assert.Equal("Hi", description.Source);
    }

    [Fact]
    public void Describe_Tag_UsesTagCategoryAndOmitsAnAbsentValue()
    {
        var withValue = _projection.Describe(new CustomTag("mood", "happy", new SourceSpan(0, 5)));
        Assert.Equal("Tag (custom)", withValue.Label);
        Assert.Equal("tag", withValue.Category);
        Assert.Contains(withValue.Attributes, a => a.Name == "value" && a.Value == "happy");

        var withoutValue = _projection.Describe(new ReservedTag("hidden", null, new SourceSpan(0, 5)));
        Assert.Equal("Tag (reserved)", withoutValue.Label);
        Assert.Equal("tag", withoutValue.Category);
        Assert.DoesNotContain(withoutValue.Attributes, a => a.Name == "value");
    }

    [Fact]
    public void Describe_SpeakerDeclaration_IncludesIdOnlyWhenPresent()
    {
        var span = new SourceSpan(0, 5);

        var withId = _projection.Describe(new SpeakerDeclaration("Alice", "alice", [], span));
        Assert.Equal("Speaker (declaration)", withId.Label);
        Assert.Equal("speech", withId.Category);
        Assert.Contains(withId.Attributes, a => a.Name == "id" && a.Value == "alice");

        var withoutId = _projection.Describe(new SpeakerDeclaration("Alice", null, [], span));
        Assert.DoesNotContain(withoutId.Attributes, a => a.Name == "id");
    }

    [Fact]
    public void Neighbors_Line_YieldsSpeakerThenSpeech()
    {
        var span = new SourceSpan(0, 5);
        var speaker = new SpeakerNameReference("Alice", span);
        var text = new Text("Hi", span);
        var line = new Line(speaker, [text], span);

        Assert.Equal(new object[] { speaker, text }, _projection.Neighbors(line));
    }

    [Fact]
    public void Neighbors_LineWithoutSpeaker_YieldsOnlySpeech()
    {
        var span = new SourceSpan(0, 2);
        var text = new Text("Hi", span);

        Assert.Equal(new object[] { text }, _projection.Neighbors(new Line(null, [text], span)));
    }

    [Fact]
    public void Neighbors_Leaf_IsEmpty()
    {
        Assert.Empty(_projection.Neighbors(new Text("Hi", new SourceSpan(0, 2))));
    }

    [Fact]
    public void Describe_DefaultSpeaker_LabelsItDefaultWithSpeechCategory()
    {
        var description = _projection.Describe(new DefaultSpeaker(SourceSpan.EmptyAt(3)));

        Assert.Equal("Speaker (default)", description.Label);
        Assert.Equal("speech", description.Category);
        Assert.Contains(description.Attributes, a => a.Name == "span");
    }

    [Fact]
    public void Describe_EmptySpanNode_HasNoSource()
    {
        // A synthetic node (a filled default speaker) marks a zero-width position, so it
        // has no source to slice — null, not an empty string that renders as a blank block.
        var description = _projection.Describe(new DefaultSpeaker(SourceSpan.EmptyAt(3)));

        Assert.Null(description.Source);
    }

    [Fact]
    public void Describe_Jump_LabelsItWithTargetAndLabel()
    {
        var span = new SourceSpan(0, 8);
        var jump = _projection.Describe(new Jump("scene-2", [new Text("Go", span)], span));

        Assert.Equal("Jump", jump.Label);
        Assert.Equal("jump", jump.Category);
        Assert.Contains(jump.Attributes, a => a.Name == "target" && a.Value == "scene-2");
        Assert.Contains(jump.Attributes, a => a.Name == "label" && a.Value == "Go");
    }

    [Fact]
    public void Neighbors_Jump_YieldsItsLabelFragments()
    {
        var span = new SourceSpan(0, 2);
        var label = new Text("Go", span);

        Assert.Equal(new object[] { label }, _projection.Neighbors(new Jump("t", [label], span)));
    }

    [Fact]
    public void Constructor_UsesGivenTitleAndDescription()
    {
        var projection = new DialogueAstProjection("Hi there", "Desugared AST", "the normalized tree");

        Assert.Equal("Desugared AST", projection.Title);
        Assert.Equal("the normalized tree", projection.Description);
    }
}
