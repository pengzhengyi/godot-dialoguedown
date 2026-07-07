using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Superpower token parsers shared across the DSL grammars, so a single definition
/// of a common token (like a quoted string) serves every grammar.
/// </summary>
internal static class SuperpowerPrimitives
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
