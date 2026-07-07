using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// The grammar that recognizes a speaker prefix at the start of a line and reports
/// its parts as <see cref="SpeakerPrefixData"/> — an optional name, an optional id,
/// and any tags. It only recognizes shape; a separate builder classifies and
/// validates. A failed parse means the text is not a speaker prefix.
/// </summary>
internal static class SpeakerPrefixParser
{
    private static readonly IParser<string> _name = SuperpowerParser.Wrap(
        ParserPrimitives.QuotedString.Try()
            .Or(Identifier.CStyle.Select(name => name.ToStringValue())));

    private static readonly IParser<string> _id = SuperpowerParser.Wrap(
        Character.EqualTo('@').IgnoreThen(Identifier.CStyle.Select(id => id.ToStringValue())));

    private static readonly IParser<char[]> _whitespace =
        SuperpowerParser.Wrap(Character.WhiteSpace.Many());

    private static readonly IParser<char> _colon =
        SuperpowerParser.Wrap(Character.EqualTo(':'));

    private static readonly IParser<Spanned<TagData>> _spacedTag =
        from _ in _whitespace
        from tag in TagParser.Token.Located()
        select tag;

    public static IParser<SpeakerPrefixData> Prefix { get; } =
        from name in _name.Optional()
        from _afterName in _whitespace
        from id in _id.Optional()
        from tags in _spacedTag.Repeated()
        from _beforeColon in _whitespace
        from _colonChar in _colon
        select new SpeakerPrefixData(name, id, tags);
}
