using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Semantics;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SceneTreeProjectionTests
{
    private readonly SceneTreeProjection _projection = new();

    [Fact]
    public void Title_IsSemanticModel() => Assert.Equal("Semantic Model", _projection.Title);

    [Fact]
    public void Description_IsANonEmptyOneLiner()
    {
        Assert.False(string.IsNullOrWhiteSpace(_projection.Description));
        Assert.DoesNotContain('\n', _projection.Description);
    }

    [Fact]
    public void Describe_Root_HasNoEntityKey()
    {
        var description = _projection.Describe(Scene.Root());

        Assert.Equal("Document root", description.Label);
        Assert.Null(description.EntityKey);
        Assert.Equal("document", description.Category);
    }

    [Fact]
    public void Describe_AnchoredScene_CarriesLabelAnchorLevelAndKey()
    {
        var heading = new SceneHeading([new Text("The Market", new SourceSpan(0, 1))], 2, new SourceSpan(0, 1));
        var scene = Scene.ForHeading(heading, "the-market");

        var description = _projection.Describe(scene);

        Assert.Equal("The Market", description.Label);
        Assert.Equal("structure", description.Category);
        Assert.Equal("scene:the-market", description.EntityKey);
        Assert.Contains(description.Attributes, attribute => attribute is { Name: "anchor", Value: "#the-market" });
        Assert.Contains(description.Attributes, attribute => attribute is { Name: "level", Value: "2" });
    }

    [Fact]
    public void Neighbors_AreTheChildScenes()
    {
        var root = Scene.Root();
        var child = Scene.ForHeading(
            new SceneHeading([new Text("A", new SourceSpan(0, 1))], 1, new SourceSpan(0, 1)), "a");
        root.AddChild(child);

        Assert.Equal([child], _projection.Neighbors(root));
    }
}
