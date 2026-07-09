namespace DialogueDown.Common;

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

    /// <summary>
    /// The span reaching from <paramref name="start"/>'s beginning through
    /// <paramref name="end"/>'s ending — used to enclose a run of nodes by their first
    /// and last spans. Unlike joining contiguous ranges, the two spans may be separated
    /// by others in between, so this covers the gap rather than rejecting it; that is why
    /// it is a named method, not a <c>+</c> operator. <paramref name="end"/> must follow
    /// <paramref name="start"/> — beginning and ending no earlier — so the ordering
    /// assumption stays explicit; a reversed pair throws.
    /// </summary>
    public static SourceSpan Covering(SourceSpan start, SourceSpan end)
    {
        if (end.Start < start.Start || end.End < start.End)
        {
            throw new ArgumentException(
                $"Cannot cover from [{start.Start}, {start.End}) through "
                + $"[{end.Start}, {end.End}); the end span must not precede the start span.");
        }

        return new SourceSpan(start.Start, end.End - start.Start);
    }
}
