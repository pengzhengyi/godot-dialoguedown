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

    /// <summary>DLG1101 — a speaker prefix binds tags but names no speaker.</summary>
    public static readonly DiagnosticDescriptor TagsWithoutSpeaker = new(
        "DLG1101",
        "Tags without a speaker",
        "\"{0}\" has tags but names no speaker for them to attach to. Begin the line with a name "
            + "to declare a speaker (Alice #excited:), or with an @id to add tags to an "
            + "already-declared one (@alice #excited:).",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1102 — a code span is not a valid game call.</summary>
    public static readonly DiagnosticDescriptor NotAGameCall = new(
        "DLG1102",
        "Not a game call",
        "\"{0}\" is not a game call. Write a query that reads a value (\"key\"), a default command "
            + "((\"do something\")), or a named command (Name(\"arg\", ...)).",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1103 — a functional element appears inside a label or alt text.</summary>
    public static readonly DiagnosticDescriptor DisallowedLabelElement = new(
        "DLG1103",
        "Disallowed element in a label",
        "{0} is not allowed inside a label or alt text; only text and styling are.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    // Semantic — DLG2xxx: a meaning-level conflict found during analysis.

    /// <summary>DLG2001 — two headings slug to the same anchor.</summary>
    public static readonly DiagnosticDescriptor DuplicateAnchor = new(
        "DLG2001",
        "Duplicate scene anchor",
        "Two scenes resolve to the same anchor '#{0}'. Rename one heading so each jump target is "
            + "unambiguous.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2002 — a heading has no sluggable text to form an anchor.</summary>
    public static readonly DiagnosticDescriptor HeadingWithoutAnchor = new(
        "DLG2002",
        "Heading without an anchor",
        "A heading needs at least one letter or number so it can be a jump target; this one has "
            + "none. Add sluggable text to the heading.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2003 — a name and an <c>@id</c> already name different speakers.</summary>
    public static readonly DiagnosticDescriptor SpeakerNameIdConflict = new(
        "DLG2003",
        "Ambiguous speaker binding",
        "Cannot bind name '{0}' to id '@{1}': both are already in use as separate speakers, so "
            + "joining them now is ambiguous. If they are the same speaker, declare it "
            + "(Name @{1}: …) before either is used on its own.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2004 — an <c>@id</c> is already bound to another speaker name.</summary>
    public static readonly DiagnosticDescriptor IdBoundToAnotherName = new(
        "DLG2004",
        "Id bound to two names",
        "id '@{0}' is already bound to speaker '{1}', so it cannot also be bound to '{2}'. Use a "
            + "different id for '{2}'.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2005 — a speaker name is already bound to another <c>@id</c>.</summary>
    public static readonly DiagnosticDescriptor NameBoundToAnotherId = new(
        "DLG2005",
        "Name bound to two ids",
        "Speaker '{0}' is already bound to id '@{1}', so it cannot also be bound to id '@{2}'. "
            + "Give the speaker a single id.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2006 — more than one speaker is marked <c>##default</c>.</summary>
    public static readonly DiagnosticDescriptor MultipleDefaultSpeakers = new(
        "DLG2006",
        "More than one default speaker",
        "Two speakers are marked ##default ('{0}' and '{1}'); only one default speaker is allowed.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2007 — a stable <c>@id</c> is used but never given a name.</summary>
    public static readonly DiagnosticDescriptor UnnamedSpeakerId = new(
        "DLG2007",
        "Unnamed speaker id",
        "Speaker '@{0}' is used but never declared with a name. Declare it with a name "
            + "(Name @{0}: …) — a stable id must belong to a named speaker.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2008 — a <c>##reserved</c> tag name is not one DialogueDown knows.</summary>
    public static readonly DiagnosticDescriptor UnknownReservedTag = new(
        "DLG2008",
        "Unknown reserved tag",
        "'##{0}' is not a known reserved tag. Use a custom tag ('#{0}') or one of DialogueDown's "
            + "reserved tags.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2009 — a jump targets a local anchor that no scene owns.</summary>
    public static readonly DiagnosticDescriptor MissingScene = new(
        "DLG2009",
        "Jump to a missing scene",
        "Jump target '#{0}' does not match any scene. Check the anchor, or add a heading it can "
            + "point to.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);
}
