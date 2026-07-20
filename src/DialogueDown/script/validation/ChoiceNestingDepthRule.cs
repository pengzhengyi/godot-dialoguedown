using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns at the first choice group beyond the recommended nesting depth on each branch.
/// Deeper descendants do not repeat the same advice; separately over-nested sibling branches
/// still report independently.
/// </summary>
internal sealed class ChoiceNestingDepthRule : DiagnosticRule
{
    private const int DefaultMaximumNestingLevel = 3;
    private readonly int _maximumNestingLevel;

    public ChoiceNestingDepthRule(int maximumNestingLevel = DefaultMaximumNestingLevel)
    {
        if (maximumNestingLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumNestingLevel),
                maximumNestingLevel,
                "Maximum choice nesting level must be positive.");
        }

        _maximumNestingLevel = maximumNestingLevel;
    }

    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.DeeplyNestedChoiceBranch;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var choices in nodes.OfType<Choices>())
        {
            var enclosingChoiceCount =
                nodes.AncestorsOf(choices).Count(node => node is Choices);
            if (enclosingChoiceCount == _maximumNestingLevel)
            {
                report(
                    SourceSpan.EmptyAt(choices.Span.Start),
                    enclosingChoiceCount + 1,
                    _maximumNestingLevel);
            }
        }
    }
}
