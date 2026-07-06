namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A half-open character range <c>[Start, End)</c> in the source, used while
/// parsing. Unlike <see cref="DialogueDown.Common.SourceSpan"/>, it may be empty
/// (a <see cref="Length"/> of zero), because a parser can match no characters — for
/// example an optional element that is absent. It is converted to a
/// <c>SourceSpan</c> only when a matched range becomes an AST node.
/// </summary>
internal readonly record struct TextRange
{
    public TextRange(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start), start, "Text range start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), length, "Text range length must be non-negative.");
        }

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;
}
