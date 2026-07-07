using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Parses a single tag token into <see cref="TagData"/>: custom (<c>#name</c>) or
/// reserved (<c>##name</c>), with an optional group value (<c>=value</c>).
/// Composable, so grammars like the speaker prefix and image alt text can consume a
/// tag and continue. It only recognizes the tag; the builder makes the AST node.
/// </summary>
internal static class TagParser
{
    private static readonly TextParser<string> _tagName =
        ParserPrimitives.QuotedString.Try()
            .Or(Identifier.CStyle.Select(name => name.ToStringValue()));

    private static readonly TextParser<bool> _reservedPrefix =
        Span.EqualTo("##").Value(true).Try()
            .Or(Character.EqualTo('#').Value(false));

    private static readonly TextParser<string?> _optionalGroupValue =
        Character.EqualTo('=')
            .IgnoreThen(_tagName.Select(name => (string?)name))
            .OptionalOrDefault();

    public static IParser<TagData> Token { get; } = SuperpowerParser.Wrap(
        from isReserved in _reservedPrefix
        from name in _tagName
        from value in _optionalGroupValue
        select new TagData(isReserved, name, value));
}
