using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns when a single line has more than one jump. Several jumps on a line chain — each runs
/// after the previous finishes — which is valid but easy to misread, so this is a readability
/// warning, not an error. Purely structural: it counts the <see cref="Jump"/> fragments directly
/// in each <see cref="Line"/>'s speech, so a jump nested inside a jump's own label does not count.
/// </summary>
internal sealed class MultipleJumpsOnLineRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } = DiagnosticCatalog.MultipleJumpsOnLine;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var line in nodes.OfType<Line>())
        {
            var jumpCount = line.Speech.OfType<Jump>().Count();
            if (jumpCount > 1)
            {
                report(line.Span, jumpCount);
            }
        }
    }
}
