namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// One diagnostic in the shape the Language Server Protocol defines, so the same value serves two
/// transports unchanged: it rides the report payload today and a future language server would
/// publish it verbatim. It carries a zero-based <see cref="Range"/>, an integer
/// <see cref="Severity"/>, the diagnostic's <see cref="Code"/> and rendered <see cref="Message"/>,
/// and the producing <see cref="Source"/> (<c>"dialoguedown"</c>). Projected from the core
/// <see cref="DialogueDown.Diagnostics.LocatedDiagnostic"/> by <see cref="DiagnosticProjection"/>.
/// </summary>
internal sealed record LspDiagnostic(
    LspRange Range,
    LspSeverity Severity,
    string Code,
    string Message,
    string Source);
