namespace DialogueSystem.Markdown;

/// <summary>
/// A half-open character range <c>[Start, End)</c> into the script source.
/// Every Markdown AST node carries one so raw text can be sliced from the
/// original source and diagnostics can point back at the exact location. The
/// range always covers at least one character.
/// </summary>
internal readonly record struct SourceSpan
{
    public SourceSpan(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start), start, "Source span start must be non-negative.");
        }

        if (length < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), length, "Source span length must be positive.");
        }

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;
}
