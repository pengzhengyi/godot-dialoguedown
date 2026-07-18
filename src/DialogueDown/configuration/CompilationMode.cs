namespace DialogueDown.Configuration;

/// <summary>
/// How far a compile proceeds after an error. Reporting-and-recovering lets a stage continue past
/// a problem, but continuing blindly through every stage piles cascading noise onto unreliable
/// material — so the caller chooses the policy.
/// </summary>
public enum CompilationMode
{
    /// <summary>
    /// Stop at the first error, throwing a <c>DiagnosticException</c> that carries it — the quickest
    /// "is it broken?" answer.
    /// </summary>
    FailFast,

    /// <summary>
    /// Within a stage, recover and collect every error; at the stage boundary, halt if that stage
    /// reported any error, because its output no longer reliably feeds the next stage. The default.
    /// </summary>
    StageBoundary,

    /// <summary>Recover through every stage and collect everything, for the fullest picture.</summary>
    BestEffort,
}
