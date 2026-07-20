namespace DialogueDown.Configuration;

/// <summary>
/// The author-facing names of the <see cref="CompilationMode"/>s a tool lets a project choose — the
/// kebab-case vocabulary shared by the CLI's <c>--mode</c> option and the <c>dialogue.toml</c>
/// <c>mode</c> key, so the two channels never drift. <see cref="CompilationMode.FailFast"/> is
/// deliberately absent: it throws at the first error instead of collecting diagnostics, so it is an
/// embedding contract a caller opts into in code, not a reporting mode a tool exposes.
/// </summary>
public static class CompilationModes
{
    private static readonly IReadOnlyDictionary<string, CompilationMode> _byName =
        new Dictionary<string, CompilationMode>(StringComparer.Ordinal)
        {
            ["stage-boundary"] = CompilationMode.StageBoundary,
            ["best-effort"] = CompilationMode.BestEffort,
        };

    /// <summary>
    /// The settable mode names as a human-readable list for help text and error messages, e.g.
    /// <c>'stage-boundary' or 'best-effort'</c>.
    /// </summary>
    public static string SettableNamesDescription { get; } =
        string.Join(" or ", _byName.Keys.Select(name => $"'{name}'"));

    /// <summary>Maps an author-facing name to its mode, or null when it is not a settable mode.</summary>
    public static CompilationMode? TryParse(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _byName.TryGetValue(name, out var mode) ? mode : null;
    }
}
