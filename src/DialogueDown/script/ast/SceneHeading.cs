using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A heading that marks the start of a scene. <see cref="Title"/> is the heading's
/// inline content as fragments, so it can carry styling or tags; <see cref="Level"/> is
/// the heading depth (1 through 6). It is a flat marker, not a container: grouping the
/// blocks that follow into a nested scene, and resolving a jump to a heading, are left
/// to a later stage.
/// </summary>
internal sealed record SceneHeading : Block
{
    public SceneHeading(IReadOnlyList<InlineFragment> title, int level, SourceSpan span)
        : base(span)
    {
        AssertValidLevel(level);
        Title = title;
        Level = level;
    }

    public IReadOnlyList<InlineFragment> Title { get; }

    public int Level { get; }

    private static void AssertValidLevel(int level)
    {
        if (level is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level), level, "Scene heading level must be between 1 and 6.");
        }
    }
}
