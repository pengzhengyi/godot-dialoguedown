using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds one <see cref="Line"/> from a group of Markdown inlines — a paragraph, or one
/// slice of it between hard breaks. It splits an optional speaker off the leading text
/// with the <see cref="SpeakerBuilder"/>, then builds the remaining speech through the
/// <see cref="InlineBuilder"/>. The line's span covers the whole group, the speaker
/// prefix included. The group must be non-empty; an empty line is dropped upstream.
/// </summary>
internal sealed class LineBuilder(SpeakerBuilder speakerBuilder, InlineBuilder inlineBuilder)
{
    public Line Build(IReadOnlyList<MarkdownInline> group)
    {
        if (group.Count == 0)
        {
            throw new ArgumentException(
                "A line must be built from at least one inline.", nameof(group));
        }

        var span = SourceSpan.Covering(group[0].Span, group[^1].Span);
        var (speaker, speech) = SplitSpeakerAndSpeech(group);
        return new Line(speaker, inlineBuilder.Build(speech), span);
    }

    // The speech inlines: the leftover leading text (if any), then the rest of the group.
    private static IReadOnlyList<MarkdownInline> AssembleSpeechInlines(
        TextInline? speechHead, IReadOnlyList<MarkdownInline> group)
    {
        var speech = new List<MarkdownInline>();
        if (speechHead is not null)
        {
            speech.Add(speechHead);
        }

        speech.AddRange(group.Skip(1));
        return speech;
    }

    private (Speaker?, IReadOnlyList<MarkdownInline>) SplitSpeakerAndSpeech(
        IReadOnlyList<MarkdownInline> group) =>
        TryBuildSpeaker(group[0], out var speaker, out var speechHead)
            ? (speaker, AssembleSpeechInlines(speechHead, group))
            : (null, group);

    // True when the leading inline is a speaker prefix. On success, `speaker` is the parsed
    // speaker and `speechHead` is the leftover text after the prefix (re-anchored), or null
    // when the prefix consumed the whole leading text. A prefix that binds tags but names no
    // speaker throws (surfaced from the speaker builder).
    private bool TryBuildSpeaker(
        MarkdownInline leadingInline, out Speaker? speaker, out TextInline? speechHead)
    {
        speaker = null;
        speechHead = null;
        if (leadingInline is not TextInline leading)
        {
            return false;
        }

        var input = new ParseInput(leading.Text, leading.Span.Start);
        var result = speakerBuilder.Build(input);
        if (!result.Success)
        {
            return false;
        }

        speaker = result.MatchedValue;
        var remainder = input.Advance(result.MatchedLength);
        speechHead = remainder.Text.Length > 0
            ? new TextInline(remainder.Text, new SourceSpan(remainder.Position, remainder.Text.Length))
            : null;
        return true;
    }
}
