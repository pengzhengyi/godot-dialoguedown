using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics.Errors;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Resolves each <c>Jump</c>'s target against the <see cref="AnchorTable"/>, producing a
/// per-jump <see cref="JumpResolution"/>. A local anchor resolves to its scene, or is a hard
/// error when no scene slugs to it; a target that names a file is deferred; an empty target is
/// left unresolved.
/// </summary>
internal static class JumpResolver
{
    /// <summary>Resolves every jump in <paramref name="jumps"/> against <paramref name="anchors"/>.</summary>
    public static IReadOnlyDictionary<Jump, JumpResolution> Resolve(
        IEnumerable<Jump> jumps, AnchorTable anchors) =>
        jumps.ToDictionary(jump => jump, jump => Resolve(jump, anchors));

    private static JumpResolution Resolve(Jump jump, AnchorTable anchors)
    {
        var target = JumpTarget.Parse(jump.Target);

        if (target.HasFilePart)
        {
            // TODO(cross-file, #59): resolve the file part against other documents, including a
            // path that names the current file; until then a file-scoped target is deferred.
            return new FileScopedJump(target.File!, target.Anchor);
        }

        if (!target.HasAnchor)
        {
            return new UnresolvedJump();
        }

        if (anchors.TryResolve(target.Anchor!, out var scene))
        {
            return new SceneJump(scene);
        }

        throw new DialogueSemanticError(
            $"Jump target '#{target.Anchor}' does not match any scene. Check the anchor, or add "
            + "a heading it can point to.", jump.Span);
    }
}
