using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

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
    public static (Scene Root, AnchorTable Anchors) Build(
        DesugaredScriptDocument document, IDiagnosticSink diagnostics)
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
                OpenScene(heading, openScenes, anchors, diagnostics);
            }
            else
            {
                openScenes.Peek().AddBlock(block);
            }
        }

        return (root, anchors);
    }

    private static void OpenScene(
        SceneHeading heading, Stack<Scene> openScenes, AnchorTable anchors, IDiagnosticSink diagnostics)
    {
        // A scene nests under the nearest shallower one, so close every open scene at this
        // heading's level or deeper. The root (level 0) always remains, so the stack never
        // empties.
        while (openScenes.Peek().Level >= heading.Level)
        {
            openScenes.Pop();
        }

        var anchor = Slug.From(heading.Title.PlainText());
        var scene = Scene.ForHeading(heading, anchor);

        // A heading whose title is all punctuation slugs to nothing, so it can never be a jump
        // target. Recovery: keep the scene in the tree but register no anchor for it.
        if (anchor.Length == 0)
        {
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.HeadingWithoutAnchor, heading.Span, []));
        }
        else
        {
            anchors.Add(anchor, scene, heading.Span, diagnostics);
        }

        openScenes.Peek().AddChild(scene);
        openScenes.Push(scene);
    }
}
