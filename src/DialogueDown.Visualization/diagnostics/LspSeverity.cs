namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// How serious an <see cref="LspDiagnostic"/> is, using the Language Server Protocol's own numbers
/// so the serialized payload matches LSP exactly. <see cref="Hint"/> has no core counterpart
/// (<see cref="DialogueDown.Diagnostics.DiagnosticSeverity"/> stops at <c>Error</c>); it exists only
/// for a future language server. <see cref="LspDiagnostic.Severity"/> forces numeric serialization,
/// so these members reach the payload as their integer values, not their names.
/// </summary>
internal enum LspSeverity
{
    /// <summary>An error: the script is invalid and must be fixed.</summary>
    Error = 1,

    /// <summary>A warning: the script compiles but is suspect.</summary>
    Warning = 2,

    /// <summary>An informational note: nothing is wrong, but something is worth pointing out.</summary>
    Information = 3,

    /// <summary>A hint: a subtle suggestion, reserved for a future language server.</summary>
    Hint = 4,
}
