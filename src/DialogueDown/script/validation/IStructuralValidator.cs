using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// The structural validation pass: it inspects a desugared document and reports structural
/// problems into a sink, running between desugar and semantic analysis. It is a facade
/// collaborator like the other stages, so the compiler depends on this seam rather than a
/// concrete rule set.
/// </summary>
internal interface IStructuralValidator
{
    /// <summary>Validates <paramref name="document"/>, reporting findings into <paramref name="diagnostics"/>.</summary>
    void Validate(DesugaredScriptDocument document, IDiagnosticSink diagnostics);
}
