using System.Globalization;

namespace DialogueDown.Diagnostics;

/// <summary>
/// The public, located view of one diagnostic: its <see cref="Code"/>, <see cref="Severity"/>, the
/// fully rendered <see cref="Message"/>, and the one-based <see cref="Start"/> and
/// <see cref="End"/> positions. Consumers — the CLI errata renderer today, the LSP and web overlays
/// later — depend on this stable projection rather than the compiler's internal diagnostic, whose
/// types are free to evolve. Built by projecting a <see cref="Diagnostic"/> through a
/// <see cref="LineMap"/>: the message is composed once (invariant culture) and the offsets are
/// resolved to line/column, so every consumer shares identical text and locations.
/// </summary>
public sealed record LocatedDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    LinePosition Start,
    LinePosition End)
{
    internal static LocatedDiagnostic Project(Diagnostic diagnostic, LineMap map)
    {
        object?[] arguments = [.. diagnostic.MessageArguments];
        var message = string.Format(
            CultureInfo.InvariantCulture, diagnostic.Descriptor.MessageFormat, arguments);

        return new LocatedDiagnostic(
            diagnostic.Descriptor.Code,
            diagnostic.Severity,
            message,
            map.Locate(diagnostic.Span.Start),
            map.Locate(diagnostic.Span.End));
    }
}
