using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The "assemble jumps" rule over a single fragment sequence: it folds a
/// <see cref="JumpIndicator"/> and the <see cref="Link"/> that follows it (across
/// same-line whitespace) into one <see cref="Jump"/>, whose span reaches from the
/// <c>=&gt;</c> through the link. A jump is single-line: a <see cref="LineBreak"/> between
/// the <c>=&gt;</c> and the link ends the jump. A <see cref="JumpIndicator"/> with no link
/// after it is dangling: it is just the characters <c>=&gt;</c>, so it degrades to a plain
/// <see cref="Text"/>. A link with no preceding indicator is left untouched. Fragments stay
/// granular here — a later stage folds adjacent text runs together — so the degraded arrow
/// is left as its own run, exactly like any other neighbouring text. It works one level at
/// a time; nested sequences are reached by the rewriter.
/// </summary>
internal static class JumpAssembler
{
    public static IReadOnlyList<InlineFragment> Assemble(IReadOnlyList<InlineFragment> fragments)
    {
        var result = new List<InlineFragment>();
        var index = 0;
        while (index < fragments.Count)
        {
            if (fragments[index] is JumpIndicator indicator)
            {
                var linkIndex = LinkIndexAfterBlanks(fragments, index + 1);
                if (linkIndex >= 0)
                {
                    var link = (Link)fragments[linkIndex];
                    result.Add(new Jump(
                        link.Target, link.Label, SourceSpan.Covering(indicator.Span, link.Span)));
                    index = linkIndex + 1;
                }
                else
                {
                    // Dangling: not a jump, so the arrow is just the characters "=>".
                    result.Add(new Text("=>", indicator.Span));
                    index++;
                }
            }
            else
            {
                result.Add(fragments[index]);
                index++;
            }
        }

        return result;
    }

    // The index of the Link that follows `start` across blank, same-line fragments, or -1
    // when the next meaningful fragment is not a link. A line break is not blank, so it
    // stops the scan and keeps the jump single-line.
    private static int LinkIndexAfterBlanks(IReadOnlyList<InlineFragment> fragments, int start)
    {
        var index = start;
        while (index < fragments.Count && fragments[index].IsBlank())
        {
            index++;
        }

        return index < fragments.Count && fragments[index] is Link ? index : -1;
    }
}
