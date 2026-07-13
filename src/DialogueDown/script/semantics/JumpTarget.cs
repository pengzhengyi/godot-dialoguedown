using DialogueDown.Common;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// A parsed jump target: an optional <see cref="File"/> part and an optional <see cref="Anchor"/>
/// part, split from a <c>Jump</c>'s raw target string at the first <c>#</c>. A null
/// <see cref="File"/> means the target is same-file (an anchor in the current document); a null
/// <see cref="Anchor"/> means no anchor was given. Both null is an empty target that points
/// nowhere.
/// </summary>
internal readonly record struct JumpTarget(string? File, string? Anchor)
{
    /// <summary>Whether the target names a file (so resolving it needs cross-file support).</summary>
    public bool HasFilePart => File is not null;

    /// <summary>Whether the target carries an anchor part.</summary>
    public bool HasAnchor => Anchor is not null;

    /// <summary>Splits <paramref name="target"/> into its file and anchor parts at the first <c>#</c>.</summary>
    public static JumpTarget Parse(string target)
    {
        var hash = target.IndexOf('#');
        return hash < 0
            ? new JumpTarget(target.NullIfEmpty(), Anchor: null)
            : new JumpTarget(target[..hash].NullIfEmpty(), target[(hash + 1)..].NullIfEmpty());
    }
}
