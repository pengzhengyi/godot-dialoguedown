using System.Diagnostics.CodeAnalysis;
using DialogueDown.Common;
using DialogueDown.Script.Semantics.Errors;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Maps each scene's slug <see cref="Scene.Anchor"/> to its <see cref="Scene"/>, so a
/// same-file jump resolves its target by anchor. An anchor is a jump target, so it must be
/// unambiguous: two scenes that slug the same are a conflict and throw, rather than the silent
/// disambiguation GitHub applies to duplicate headings.
/// </summary>
internal sealed class AnchorTable
{
    private readonly Dictionary<string, Scene> _sceneByAnchor = [];

    /// <summary>The scene for <paramref name="anchor"/>, or null when no scene slugs to it.</summary>
    public bool TryResolve(string anchor, [MaybeNullWhen(false)] out Scene scene) =>
        _sceneByAnchor.TryGetValue(anchor, out scene);

    internal void Add(string anchor, Scene scene, SourceSpan span)
    {
        if (!_sceneByAnchor.TryAdd(anchor, scene))
        {
            throw new DialogueSemanticError(
                $"Two scenes resolve to the same anchor '#{anchor}'. Rename one heading so "
                + "each jump target is unambiguous.", span);
        }
    }
}
