using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader;

namespace DialogueDown.Cli;

/// <summary>
/// Resolves the <see cref="CompilerOptions"/> a command uses: an explicit <c>--config</c> path,
/// else the nearest <c>dialogue.toml</c> found by walking up from a starting directory, else
/// <see cref="CompilerOptions.Default"/>. Discovery mirrors how established tools (tsc,
/// clang-format, Prettier, Black) find their config — nearest wins — so one file at a project
/// root serves scripts nested in subfolders. It is the only CLI type that touches the TOML
/// loader, keeping the file dependency at the edge.
/// </summary>
internal sealed class ProjectConfiguration
{
    /// <summary>The configuration file the CLI discovers by walking up the directory tree.</summary>
    public const string FileName = "dialogue.toml";

    /// <summary>
    /// Resolves the options for a compile. An <paramref name="explicitConfigPath"/> (the
    /// <c>--config</c> value) is loaded directly; otherwise the nearest <see cref="FileName"/> at
    /// or above <paramref name="startDirectory"/> is loaded, and the search stops at
    /// <paramref name="boundaryDirectory"/> when given (it is never read above the boundary).
    /// Nothing found yields <see cref="CompilerOptions.Default"/>.
    /// </summary>
    public CompilerOptions Resolve(
        string? explicitConfigPath, string startDirectory, string? boundaryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        if (!string.IsNullOrWhiteSpace(explicitConfigPath))
        {
            return TomlConfigurationLoader.Load(explicitConfigPath);
        }

        var discovered = Discover(startDirectory, boundaryDirectory);
        return discovered is null ? CompilerOptions.Default : TomlConfigurationLoader.Load(discovered);
    }

    private static string? Discover(string startDirectory, string? boundaryDirectory)
    {
        var boundary = boundaryDirectory is null ? null : Path.GetFullPath(boundaryDirectory);
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
             directory is not null && IsWithinBoundary(directory.FullName, boundary);
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    // The walk climbs only within the boundary subtree, so a config above the boundary — the
    // served root's parent — is never read. A null boundary climbs to the filesystem root.
    private static bool IsWithinBoundary(string directory, string? boundary)
    {
        if (boundary is null)
        {
            return true;
        }

        var normalized = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var limit = boundary.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalized, limit, StringComparison.Ordinal)
            || normalized.StartsWith(limit + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
