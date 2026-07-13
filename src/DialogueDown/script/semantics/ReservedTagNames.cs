using System.Collections.Frozen;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The closed vocabulary of reserved (<c>##name</c>) tags that DialogueDown owns. A reserved
/// tag is meaningful only when its name is in this set; anything else is rejected by the tag
/// validator. This is the single source of truth for reserved-tag names, so the passes that
/// act on them (the speaker binder on <see cref="Default"/>, the validator on <see cref="Known"/>)
/// agree.
/// </summary>
internal static class ReservedTagNames
{
    /// <summary>Marks the speaker it rides on as the document's default speaker.</summary>
    public const string Default = "default";

    /// <summary>Every reserved-tag name DialogueDown recognizes.</summary>
    public static readonly FrozenSet<string> Known = new[] { Default }.ToFrozenSet();
}
