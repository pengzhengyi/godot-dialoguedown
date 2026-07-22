using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Recognizes and classifies a speaker prefix at the start of a line: metadata (an id
/// and/or tags) makes it a declaration, a bare name a name reference, and a bare id an
/// id reference. An <see cref="Ok"/> result carries the <see cref="Speaker"/> and how
/// much was consumed; a failure means the line has no speaker. A prefix that binds
/// metadata but names no speaker reports <see cref="DiagnosticCatalog.TagsWithoutSpeaker"/>
/// and recovers to a <see cref="DefaultSpeaker"/>, dropping the orphan tags.
/// </summary>
internal sealed class SpeakerBuilder(IParser<SpeakerPrefixData> parser, TagBuilder tagBuilder)
{
    public ParseResult<Speaker> Build(ParseInput input, IDiagnosticSink diagnostics)
    {
        var result = parser.Consume(input);
        if (result.Error is { } error)
        {
            return ParseResult<Speaker>.Fail(error);
        }

        var speaker = Classify(
            result.MatchedValue, result.MatchedRange.ToSourceSpan(), input.Text, diagnostics);
        return speaker is null
            ? ParseResult<Speaker>.Fail(new ParseError("no speaker prefix"))
            : ParseResult<Speaker>.Ok(new ParseMatch<Speaker>(speaker, result.MatchedRange));
    }

    private Speaker? Classify(
        SpeakerPrefixData data, SourceSpan span, string content, IDiagnosticSink diagnostics)
    {
        var tagNodes = data.Tags
            .Select(tag => tagBuilder.Build(tag.Value, tag.Range.ToSourceSpan()))
            .ToList();

        // The name and id sub-spans are added in a following step; the separator is always
        // present because a written prefix ends in a colon.
        var prefixSpans = new SpeakerPrefixSpans(null, null, data.SeparatorRange.ToSourceSpan());

        if (data.Name is not null)
        {
            return data.Id is not null || tagNodes.Count > 0
                ? new SpeakerDeclaration(data.Name, data.Id, tagNodes, span) { PrefixSpans = prefixSpans }
                : new SpeakerNameReference(data.Name, span) { PrefixSpans = prefixSpans };
        }

        if (data.Id is not null)
        {
            return tagNodes.Count > 0
                ? new PartialSpeakerDeclaration(data.Id, tagNodes, span) { PrefixSpans = prefixSpans }
                : new SpeakerIdReference(data.Id, span) { PrefixSpans = prefixSpans };
        }

        if (tagNodes.Count > 0)
        {
            // Tags with no speaker to attach to: report and recover by dropping the tags and
            // attributing the line to the default speaker, so the rest of the line still compiles.
            // The default speaker names no one, so it carries no prefix spans.
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.TagsWithoutSpeaker, span, [content]));
            return new DefaultSpeaker(span);
        }

        return null;
    }
}
