namespace DialogueDown.Diagnostics;

/// <summary>
/// The central inventory of every diagnostic kind DialogueDown reports. Keeping the descriptors in
/// one place makes the <c>DLG####</c> codes greppable and documentable, and lets a test enforce
/// that each code is unique — a guarantee scattered per-producer descriptors cannot give. A
/// producer or validation rule reports by referencing the descriptor it owns here.
/// </summary>
internal static class DiagnosticCatalog
{
    // Syntax — DLG1xxx: a malformed line surface, or a structural readability concern.

    /// <summary>DLG1003 — a line carries more than one jump (structural, advisory).</summary>
    public static readonly DiagnosticDescriptor MultipleJumpsOnLine = new(
        "DLG1003",
        "Multiple jumps on a line",
        "This line has {0} jumps; multiple jumps on one line run in sequence and are easy to "
            + "misread — prefer at most one.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Warning);

    // Semantic — DLG2xxx: a meaning-level conflict found during analysis.

    /// <summary>DLG2009 — a jump targets a local anchor that no scene owns.</summary>
    public static readonly DiagnosticDescriptor MissingScene = new(
        "DLG2009",
        "Jump to a missing scene",
        "Jump target '#{0}' does not match any scene. Check the anchor, or add a heading it can "
            + "point to.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);
}
