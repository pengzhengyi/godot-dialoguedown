using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A section of the script opened by a heading. <see cref="Title"/> is the heading's
/// inline content as fragments, so it can carry styling or tags. <see cref="Level"/> is
/// the heading depth (1 through 6). <see cref="Body"/> is the content beneath it in
/// source order, which may nest more scenes.
/// </summary>
internal sealed record Scene : Block
{
    public Scene(
        IReadOnlyList<SpeechFragment> title, int level, IReadOnlyList<Block> body, SourceSpan span)
        : base(span)
    {
        AssertValidLevel(level);
        Title = title;
        Level = level;
        Body = body;
    }

    public IReadOnlyList<SpeechFragment> Title { get; }

    public int Level { get; }

    public IReadOnlyList<Block> Body { get; }

    private static void AssertValidLevel(int level)
    {
        if (level is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level), level, "Scene level must be between 1 and 6.");
        }
    }
}
