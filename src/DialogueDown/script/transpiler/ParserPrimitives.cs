using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Superpower parsers shared across the transpiler's leaf parsers, so a single
/// definition of a common token (like a quoted string) serves every grammar.
/// </summary>
internal static class ParserPrimitives
{
    /// <summary>
    /// A straight-double-quoted string; the surrounding quotes are consumed and
    /// only the inner characters are returned.
    /// </summary>
    public static TextParser<string> QuotedString { get; } =
        from open in Character.EqualTo('"')
        from chars in Character.Except('"').Many()
        from close in Character.EqualTo('"')
        select new string(chars);
}
