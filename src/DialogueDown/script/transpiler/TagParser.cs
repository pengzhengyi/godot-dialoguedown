using DialogueDown.Common;
using DialogueDown.Script.Ast;
using Superpower;
using Superpower.Parsers;
using TagFromSpan =
    System.Func<DialogueDown.Common.SourceSpan, DialogueDown.Script.Ast.Tag>;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Parses a tag token into a <see cref="CustomTag"/> or a <see cref="ReservedTag"/>,
/// with an optional group value. Text that is not a valid tag is rejected with a
/// <see cref="DialogueSyntaxError"/>.
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

    private static readonly TextParser<TagFromSpan> _tag =
        from reserved in _reservedPrefix
        from name in _tagName
        from value in _optionalGroupValue
        select (TagFromSpan)(span => reserved
            ? new ReservedTag(name, value, span)
            : new CustomTag(name, value, span));

    private static readonly TextParser<TagFromSpan> _grammar = _tag.AtEnd();

    public static Tag Parse(string content, SourceSpan span)
    {
        try
        {
            return _grammar.Parse(content)(span);
        }
        catch (ParseException error)
        {
            throw new DialogueSyntaxError(BuildMessage(content), span, error);
        }
    }

    private static string BuildMessage(string content) =>
        $"""
        "{content}" is not a tag. Tags look like #name, #name=value, ##name, or ##name=value.
        """;
}
