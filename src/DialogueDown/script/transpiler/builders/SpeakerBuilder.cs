using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Errors;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Recognizes and classifies a speaker prefix at the start of a line: metadata (an id
/// and/or tags) makes it a declaration, a bare name a name reference, and a bare id an
/// id reference. An <see cref="Ok"/> result carries the <see cref="Speaker"/> and how
/// much was consumed; a failure means the line has no speaker. A prefix that binds
/// metadata but names no speaker is a <see cref="DialogueSyntaxError"/>.
/// </summary>
internal sealed class SpeakerBuilder(IParser<SpeakerPrefixData> parser, TagBuilder tagBuilder)
{
    public ParseResult<Speaker> Build(ParseInput input)
    {
        var result = parser.Consume(input);
        if (result.Error is { } error)
        {
            return ParseResult<Speaker>.Fail(error);
        }

        var speaker = Classify(result.MatchedValue, result.MatchedRange.ToSourceSpan(), input.Text);
        return speaker is null
            ? ParseResult<Speaker>.Fail(new ParseError("no speaker prefix"))
            : ParseResult<Speaker>.Ok(new ParseMatch<Speaker>(speaker, result.MatchedRange));
    }

    private static string TagsWithoutSpeakerMessage(string content) =>
        $"""
        "{content}" has tags but names no speaker for them to attach to. Begin the
        line with a name to declare a speaker (Alice #excited:), or with an @id to
        add tags to an already-declared one (@alice #excited:).
        """;

    private Speaker? Classify(SpeakerPrefixData data, SourceSpan span, string content)
    {
        var tagNodes = data.Tags
            .Select(tag => tagBuilder.Build(tag.Value, tag.Range.ToSourceSpan()))
            .ToList();

        if (data.Name is not null)
        {
            return data.Id is not null || tagNodes.Count > 0
                ? new SpeakerDeclaration(data.Name, data.Id, tagNodes, span)
                : new SpeakerNameReference(data.Name, span);
        }

        if (data.Id is not null)
        {
            return tagNodes.Count > 0
                ? new PartialSpeakerDeclaration(data.Id, tagNodes, span)
                : new SpeakerIdReference(data.Id, span);
        }

        if (tagNodes.Count > 0)
        {
            throw new DialogueSyntaxError(TagsWithoutSpeakerMessage(content), span);
        }

        return null;
    }
}
