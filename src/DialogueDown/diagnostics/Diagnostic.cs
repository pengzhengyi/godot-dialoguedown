using DialogueDown.Common;

namespace DialogueDown.Diagnostics;

/// <summary>
/// One located report found during compilation: the <see cref="Descriptor"/> defining its kind,
/// the <see cref="Span"/> in the source it points at, the <see cref="MessageArguments"/> that fill
/// the descriptor's message format, and a <see cref="Severity"/> that defaults to the descriptor's
/// <see cref="DiagnosticDescriptor.DefaultSeverity"/> unless a producer overrides it (so a later
/// configuration pass can promote or demote one). Two diagnostics are equal when they report the
/// same problem — the same descriptor, span, severity, and message arguments by value.
/// </summary>
internal sealed record Diagnostic
{
    public Diagnostic(
        DiagnosticDescriptor descriptor,
        SourceSpan span,
        IReadOnlyList<object> messageArguments,
        DiagnosticSeverity? severity = null)
    {
        Descriptor = descriptor;
        Span = span;
        MessageArguments = messageArguments;
        Severity = severity ?? descriptor.DefaultSeverity;
    }

    /// <summary>The stable definition of this diagnostic's kind.</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>The source range this diagnostic points at.</summary>
    public SourceSpan Span { get; }

    /// <summary>
    /// The values that fill the descriptor's message format, kept structured (not pre-formatted)
    /// so composing the final text stays a rendering concern.
    /// </summary>
    public IReadOnlyList<object> MessageArguments { get; }

    /// <summary>This diagnostic's severity: the descriptor's default unless a producer overrode it.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Whether this diagnostic is an <see cref="DiagnosticSeverity.Error"/> — the severity
    /// that fails a compile.</summary>
    public bool IsError => Severity == DiagnosticSeverity.Error;

    // The message arguments are compared by value (element-wise), so two diagnostics reporting the
    // same problem are equal even when built with separate argument lists — the record default
    // would compare the list by reference.
    public bool Equals(Diagnostic? other) =>
        other is not null
        && Descriptor == other.Descriptor
        && Span.Equals(other.Span)
        && Severity == other.Severity
        && MessageArguments.SequenceEqual(other.MessageArguments);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Descriptor);
        hash.Add(Span);
        hash.Add(Severity);
        foreach (var argument in MessageArguments)
        {
            hash.Add(argument);
        }

        return hash.ToHashCode();
    }
}
