using System.Globalization;

namespace DialogueDown.Diagnostics;

/// <summary>
/// The public, located view of one diagnostic: its <see cref="Code"/>, <see cref="Severity"/>,
/// <see cref="Category"/>, the fully rendered <see cref="Message"/>, the one-based
/// <see cref="Start"/> and <see cref="End"/> positions, and the half-open character range
/// <c>[<see cref="StartOffset"/>, <see cref="EndOffset"/>)</c> the diagnostic points at.
/// Line/column suit human display and editor (LSP) ranges; the offsets let a tool index the source
/// directly — a rendered underline or an editor selection. Consumers — the CLI errata renderer
/// today, the LSP and web overlays later — depend on this stable projection rather than the
/// compiler's internal diagnostic, whose types are free to evolve. Built by projecting a
/// <see cref="Diagnostic"/> through a <see cref="LineMap"/>: the message is composed once (invariant
/// culture) and the offsets are resolved to line/column, so every consumer shares identical text
/// and locations.
/// </summary>
public sealed record LocatedDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    DiagnosticCategory Category,
    string Message,
    LinePosition Start,
    LinePosition End,
    int StartOffset,
    int EndOffset)
{
    internal static LocatedDiagnostic Project(Diagnostic diagnostic, LineMap map)
    {
        object?[] arguments = [.. diagnostic.MessageArguments];
        var message = string.Format(
            CultureInfo.InvariantCulture, diagnostic.Descriptor.MessageFormat, arguments);

        var span = diagnostic.Span;
        return new LocatedDiagnostic(
            diagnostic.Descriptor.Code,
            diagnostic.Severity,
            diagnostic.Descriptor.Category,
            message,
            map.Locate(span.Start),
            map.Locate(span.End),
            span.Start,
            span.End);
    }
}
