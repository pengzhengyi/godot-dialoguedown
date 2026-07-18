using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// One structural check over a compiled tree: given the desugared tree indexed by node type, it
/// reports zero or more diagnostics into the sink. Each rule owns one descriptor and is
/// unit-testable in isolation, so rules can be added without touching the pipeline.
/// </summary>
internal interface IDiagnosticRule
{
    /// <summary>Inspects <paramref name="nodes"/> and reports findings into <paramref name="diagnostics"/>.</summary>
    void Check(DialogueTreeIndex nodes, IDiagnosticSink diagnostics);
}
