using DialogueDown.Diagnostics;
using Spectre.Console;

namespace DialogueDown.Cli;

/// <summary>
/// The one-line errata renderer: it writes each located diagnostic as a colored
/// <c>file(line,column): severity CODE: message</c> line plus a summary, through the app's
/// <see cref="IAnsiConsole"/>. Diagnostic text and paths are interpolated (so Spectre escapes any
/// markup in them), and rendering stays confined to the CLI (the umbrella note's DD7).
/// </summary>
internal sealed class ErrataRenderer(IAnsiConsole console) : IErrataRenderer
{
    public void Render(string file, IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.Count == 0)
        {
            return;
        }

        foreach (var diagnostic in Ordered(diagnostics))
        {
            var color = ColorOf(diagnostic.Severity);
            var location = $"{file}({diagnostic.Start.Line},{diagnostic.Start.Column})";
            console.MarkupLineInterpolated(
                $"[{color}]{location}: {LabelOf(diagnostic.Severity)} {diagnostic.Code}: {diagnostic.Message}[/]");
        }

        console.MarkupLineInterpolated($"[grey]{Summarize(diagnostics)}[/]");
    }

    private static IEnumerable<LocatedDiagnostic> Ordered(IReadOnlyList<LocatedDiagnostic> diagnostics) =>
        diagnostics
            .OrderBy(diagnostic => diagnostic.Start.Line)
            .ThenBy(diagnostic => diagnostic.Start.Column)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal);

    private static string ColorOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "red",
        DiagnosticSeverity.Warning => "yellow",
        _ => "cyan",
    };

    private static string LabelOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        _ => "info",
    };

    private static string Summarize(IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        var parts = new List<string>();
        Count(diagnostics, DiagnosticSeverity.Error, "error", parts);
        Count(diagnostics, DiagnosticSeverity.Warning, "warning", parts);
        Count(diagnostics, DiagnosticSeverity.Info, "info", parts);
        return string.Join(", ", parts);
    }

    private static void Count(
        IReadOnlyList<LocatedDiagnostic> diagnostics,
        DiagnosticSeverity severity,
        string noun,
        List<string> parts)
    {
        var count = diagnostics.Count(diagnostic => diagnostic.Severity == severity);
        if (count > 0)
        {
            parts.Add($"{count} {noun}{(count == 1 ? string.Empty : "s")}");
        }
    }
}
