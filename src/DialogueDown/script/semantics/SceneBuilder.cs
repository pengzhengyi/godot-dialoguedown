using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics.Errors;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Turns a desugared document's flat block list into a <see cref="Scene"/> tree and the
/// <see cref="AnchorTable"/> that indexes it. Headings nest by the standard outline rule — a
/// heading nests under the nearest shallower one — and each scene owns the blocks between its
/// heading and the next. Blocks before the first heading, and the top-level scenes, hang off a
/// headingless <b>root</b> scene, so leading content is never orphaned.
/// </summary>
internal static class SceneBuilder
{
    /// <summary>Builds the scene tree (its root) and the anchor table for <paramref name="document"/>.</summary>
    public static (Scene Root, AnchorTable Anchors) Build(DesugaredScriptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = Scene.Root();
        var anchors = new AnchorTable();

        // The chain of open ancestor scenes, innermost on top; the root anchors it so a
        // block or top-level heading always has an owner.
        var openScenes = new Stack<Scene>();
        openScenes.Push(root);

        foreach (var block in document.Body)
        {
            if (block is SceneHeading heading)
            {
                OpenScene(heading, openScenes, anchors);
            }
            else
            {
                openScenes.Peek().AddBlock(block);
            }
        }

        return (root, anchors);
    }

    private static void OpenScene(SceneHeading heading, Stack<Scene> openScenes, AnchorTable anchors)
    {
        // A scene nests under the nearest shallower one, so close every open scene at this
        // heading's level or deeper. The root (level 0) always remains, so the stack never
        // empties.
        while (openScenes.Peek().Level >= heading.Level)
        {
            openScenes.Pop();
        }

        var anchor = Slug.From(heading.Title.PlainText());
        ThrowWhenHeadingHasNoAnchor(anchor, heading);
        var scene = Scene.ForHeading(heading, anchor);
        anchors.Add(anchor, scene, heading.Span);
        openScenes.Peek().AddChild(scene);
        openScenes.Push(scene);
    }

    // A heading whose title is all punctuation slugs to nothing, so it could never be a jump
    // target. Reject it at the heading, rather than leaving a link to it silently unresolved.
    private static void ThrowWhenHeadingHasNoAnchor(string anchor, SceneHeading heading)
    {
        if (anchor.Length == 0)
        {
            throw new DialogueSemanticError(
                "A heading needs at least one letter or number so it can be a jump target; this "
                + "one has none. Add sluggable text to the heading.", heading.Span);
        }
    }
}
