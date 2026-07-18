using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Base class for a structural validation rule. It takes the boilerplate — the argument guards
/// and building each diagnostic from this rule's descriptor — so a concrete rule only walks the
/// indexed nodes and calls <c>report(span, arguments)</c> where it finds a problem.
/// </summary>
internal abstract class DiagnosticRule : IDiagnosticRule
{
    /// <summary>The kind of diagnostic this rule reports.</summary>
    protected abstract DiagnosticDescriptor Descriptor { get; }

    public void Check(DialogueTreeIndex nodes, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        // The reporter closes over the sink so a rule reports with just a span and arguments,
        // and stays stateless — the sink is never held on the rule, so a shared rule instance is
        // safe across concurrent compilations.
        Analyze(nodes, (span, arguments) =>
            diagnostics.Report(new Diagnostic(Descriptor, span, arguments)));
    }

    /// <summary>Inspects the indexed nodes and reports findings through <paramref name="report"/>.</summary>
    protected abstract void Analyze(DialogueTreeIndex nodes, Reporter report);

    /// <summary>Reports a finding of this rule at <paramref name="span"/> with message arguments.</summary>
    protected delegate void Reporter(SourceSpan span, params object[] arguments);
}
