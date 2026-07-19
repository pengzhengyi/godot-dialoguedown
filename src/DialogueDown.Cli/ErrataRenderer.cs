using DialogueDown.Diagnostics;
using Errata;
using Spectre.Console;

namespace DialogueDown.Cli;

/// <summary>
/// Renders a compile's located diagnostics. On an interactive console it uses the
/// <see href="https://github.com/spectreconsole/errata">Errata</see> library to draw a rich block
/// per diagnostic — the source line with a colored caret under the offending range — and otherwise
/// writes a greppable <c>file(line,column): severity CODE: message</c> one-liner. Both paths end
/// with a summary, and rendering stays confined to the CLI (the umbrella note's DD7).
/// </summary>
internal sealed class ErrataRenderer(IAnsiConsole console) : IErrataRenderer
{
    public void Render(string file, string source, IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.Count == 0)
        {
            return;
        }

        var ordered = Ordered(diagnostics).ToList();
        if (!console.Profile.Capabilities.Interactive || !TryRenderRich(console, file, source, ordered))
        {
            RenderPlain(console, file, ordered);
        }

        console.MarkupLineInterpolated($"[grey]{Summarize(diagnostics)}[/]");
    }

    // The rich, source-context rendering. Returns false (so the caller falls back to the one-liner)
    // if Errata cannot render a span — a failed report must never crash the compile command.
    private static bool TryRenderRich(
        IAnsiConsole console, string file, string source, IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        try
        {
            var repository = new InMemorySourceRepository();
            repository.Register(file, source);
            var report = new Report(repository);
            foreach (var diagnostic in diagnostics)
            {
                var label = new Label(file, LabelSpan(diagnostic, source), diagnostic.Code)
                    .WithColor(SpectreColorOf(diagnostic.Severity));
                report.Diagnostics.Add(ErrataDiagnosticOf(diagnostic).WithLabel(label));
            }

            report.Render(console, new ReportSettings { PropagateExceptions = true });
            console.WriteLine(); // separate the last block from the summary line
            return true;
        }
        catch (ErrataException)
        {
            return false;
        }
    }

    private static void RenderPlain(
        IAnsiConsole console, string file, IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var color = ColorOf(diagnostic.Severity);
            var location = $"{file}({diagnostic.Start.Line},{diagnostic.Start.Column})";
            console.MarkupLineInterpolated(
                $"[{color}]{location}: {LabelOf(diagnostic.Severity)} {diagnostic.Code}: {diagnostic.Message}[/]");
        }
    }

    // An Errata span within the source, non-decreasing; a zero-width (synthetic) span is widened by
    // one where possible so there is a caret to draw.
    private static TextSpan LabelSpan(LocatedDiagnostic diagnostic, string source)
    {
        var start = Math.Clamp(diagnostic.StartOffset, 0, source.Length);
        var end = Math.Clamp(diagnostic.EndOffset, start, source.Length);
        if (end == start && end < source.Length)
        {
            end = start + 1;
        }

        return new TextSpan(start, end);
    }

    private static Diagnostic ErrataDiagnosticOf(LocatedDiagnostic diagnostic)
    {
        var errata = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => Diagnostic.Error(diagnostic.Message),
            DiagnosticSeverity.Warning => Diagnostic.Warning(diagnostic.Message),
            _ => Diagnostic.Info(diagnostic.Message),
        };
        return errata.WithCode(diagnostic.Code);
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

    private static Color SpectreColorOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => Color.Red,
        DiagnosticSeverity.Warning => Color.Yellow,
        _ => Color.Aqua,
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
