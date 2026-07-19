using System.Text;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Renders the diagnostic catalog as the user-facing "Error codes" reference page. The page is a
/// generated artifact: <see cref="DiagnosticCatalogDocumentationTests"/> asserts the committed
/// <c>docs/guide/error-codes.md</c> matches this output, so the reference never drifts from the
/// catalog. Grouped by category and sorted by code, with one anchored subsection per code so a
/// tool can deep-link a diagnostic (for example, <c>#dlg2001</c>).
/// </summary>
internal static class DiagnosticCatalogMarkdown
{
    private const string Intro =
        "# Error codes\n"
        + "\n"
        + "DialogueDown reports each problem it finds as a **diagnostic** with a stable `DLG####` "
        + "code, so a message is easy to look up. A code's leading digit names its category — "
        + "`DLG1xxx` syntax, `DLG2xxx` semantic, `DLG3xxx` style — and each diagnostic has a "
        + "default **severity**: **error** (must be fixed), **warning** (compiles but is suspect), "
        + "or **info** (a neutral note). Placeholders such as `{0}` are filled with specifics — a "
        + "name, a count — when the message is shown.\n";

    // Categories in reporting order, each with the range its codes occupy and a one-line summary.
    // A category with no descriptors yet (Style) is listed here but only rendered once it has one.
    private static readonly IReadOnlyList<CategorySection> _sections =
    [
        new(DiagnosticCategory.Syntax, "DLG1xxx", "A line's surface does not parse as intended."),
        new(
            DiagnosticCategory.Semantic,
            "DLG2xxx",
            "A meaning-level problem found during analysis — a reference that does not resolve, "
            + "or a conflict."),
        new(DiagnosticCategory.Style, "DLG3xxx", "A valid script that reads correctly but could read better."),
    ];

    public static string Render()
    {
        var byCategory = DiagnosticCatalogReflection.Descriptors()
            .GroupBy(descriptor => descriptor.Category)
            .ToDictionary(group => group.Key, group => group.OrderBy(d => d.Code, StringComparer.Ordinal).ToList());

        var builder = new StringBuilder(Intro);
        foreach (var section in _sections)
        {
            if (!byCategory.TryGetValue(section.Category, out var descriptors))
            {
                continue;
            }

            AppendSection(builder, section, descriptors);
        }

        return builder.ToString();
    }

    private static void AppendSection(
        StringBuilder builder, CategorySection section, IReadOnlyList<DiagnosticDescriptor> descriptors)
    {
        builder.Append($"\n## {section.Category} (`{section.Range}`)\n\n{section.Summary}\n");
        foreach (var descriptor in descriptors)
        {
            builder.Append(
                $"\n### {descriptor.Code}\n\n**{descriptor.Title}** · {descriptor.DefaultSeverity}\n\n"
                + $"{descriptor.MessageFormat}\n");
        }
    }

    private sealed record CategorySection(DiagnosticCategory Category, string Range, string Summary);
}
