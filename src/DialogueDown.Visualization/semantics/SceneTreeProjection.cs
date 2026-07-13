using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// Projects the analyzer's <see cref="Scene"/> tree for display: the implicit root scene and
/// the scenes nested under it, each labeled by its heading text and carrying its anchor and
/// level. A scene with an anchor carries the cross-link <see cref="NodeDescription.EntityKey"/>
/// <c>scene:&lt;anchor&gt;</c>, so hovering it highlights the same scene in the anchor and
/// jump tables. Scenes reuse the <c>structure</c> category, so they share the scene-heading
/// color of the AST tabs.
/// </summary>
internal sealed class SceneTreeProjection : INodeProjection<Scene>
{
    private const string StructureCategory = "structure";
    private const string DocumentCategory = "document";

    public string Title => "Semantic Model";

    public string Description =>
        "The semantic model the analyzer resolves — the scene tree beside its speaker, "
        + "anchor, and jump-resolution tables, cross-linked so hovering a scene or speaker "
        + "highlights it everywhere.";

    public NodeDescription Describe(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Anchor is null)
        {
            return new NodeDescription(
                "Document root", category: DocumentCategory, typeName: "Document");
        }

        return new NodeDescription(
            SceneEntity.Label(scene),
            [
                new DisplayAttribute("anchor", $"#{scene.Anchor}"),
                new DisplayAttribute("level", $"{scene.Level}"),
            ],
            category: StructureCategory,
            entityKey: SceneEntity.Key(scene),
            typeName: "Scene");
    }

    public IEnumerable<Scene> Neighbors(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return scene.Children;
    }
}
