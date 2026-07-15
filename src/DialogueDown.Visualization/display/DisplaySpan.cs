namespace DialogueDown.Visualization;

/// <summary>
/// A node's source location as a half-open character range <c>[Start, End)</c> into the
/// original document — the structured form of the display "span" attribute, so a client can
/// splice an edit back into the exact source it came from. Absent for a synthetic node, which
/// has no source of its own; present for the document-root node as the whole document.
/// </summary>
public readonly record struct DisplaySpan(int Start, int End);
