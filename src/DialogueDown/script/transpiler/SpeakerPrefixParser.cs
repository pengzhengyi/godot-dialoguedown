using DialogueDown.Common;
using DialogueDown.Script.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using TagFromSpan =
    System.Func<DialogueDown.Common.SourceSpan, DialogueDown.Script.Ast.Tag>;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Recognizes a speaker prefix at the start of a line and classifies it (D11):
/// metadata (an id and/or tags) makes it a <see cref="SpeakerDeclaration"/>; a bare
/// name is a <see cref="SpeakerNameReference"/>; a bare id is a
/// <see cref="SpeakerIdReference"/>. Metadata without a name is a
/// <see cref="DialogueSyntaxError"/>. If the leading text is not a valid prefix,
/// the line has no speaker and the whole text is speech.
/// </summary>
internal static class SpeakerPrefixParser
{
    private static readonly TextParser<string> _name =
        ParserPrimitives.QuotedString.Try()
            .Or(Identifier.CStyle.Select(name => name.ToStringValue()));

    private static readonly TextParser<string?> _optionalName =
        _name.Select(name => (string?)name).OptionalOrDefault();

    private static readonly TextParser<string?> _optionalId =
        Character.EqualTo('@')
            .IgnoreThen(Identifier.CStyle.Select(id => (string?)id.ToStringValue()))
            .OptionalOrDefault();

    private static readonly TextParser<(TagFromSpan Value, TextSpan Location)[]> _tags =
        (from leading in Character.WhiteSpace.Many()
         from tag in TagParser.Token.Located()
         select tag).Try().Many();

    private static readonly TextParser<ParsedPrefix> _grammar =
        from name in _optionalName
        from afterName in Character.WhiteSpace.Many()
        from id in _optionalId
        from tags in _tags
        from beforeColon in Character.WhiteSpace.Many()
        from colon in Character.EqualTo(':')
        select new ParsedPrefix(name, id, tags);

    public static SpeakerPrefix? TryParse(string text, SourceSpan span)
    {
        var result = _grammar.TryParse(text);
        if (!result.HasValue)
        {
            return null;
        }

        var parsed = result.Value;
        var baseOffset = span.Start;
        var consumed = result.Remainder.Position.Absolute;
        var prefixSpan = new SourceSpan(baseOffset, consumed);

        var tags = parsed.Tags
            .Select(tag => tag.Value(
                new SourceSpan(baseOffset + tag.Location.Position.Absolute, tag.Location.Length)))
            .ToList();

        var speaker = Classify(parsed.Name, parsed.Id, tags, prefixSpan, text);
        return speaker is null ? null : new SpeakerPrefix(speaker, consumed);
    }

    private static Speaker? Classify(
        string? name, string? id, IReadOnlyList<Tag> tags, SourceSpan span, string content)
    {
        var hasMetadata = id is not null || tags.Count > 0;
        if (name is not null)
        {
            return hasMetadata
                ? new SpeakerDeclaration(name, id, tags, span)
                : new SpeakerNameReference(name, span);
        }

        if (id is not null && tags.Count == 0)
        {
            return new SpeakerIdReference(id, span);
        }

        if (hasMetadata)
        {
            throw new DialogueSyntaxError(NamelessMessage(content), span);
        }

        return null;
    }

    private static string NamelessMessage(string content) =>
        $"""
        "{content}" binds a speaker id or tags but names no speaker. Give the
        speaker a name, for example: Alice @A #main:.
        """;

    private sealed record ParsedPrefix(
        string? Name, string? Id, IReadOnlyList<(TagFromSpan Value, TextSpan Location)> Tags);
}
