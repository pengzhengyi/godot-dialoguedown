namespace DialogueDown.Visualization.Live;

/// <summary>
/// Lists a directory anywhere on the local filesystem for the launcher's file picker —
/// its sub-directories and its <c>.dialogue.md</c> sources, plus its parent (<c>null</c>
/// at a filesystem root) — all as absolute paths. Browsing is unconfined by design (a
/// native "Open Folder" dialog is too), but the server is loopback-only and only a
/// chosen root's files are ever served (<see cref="LauncherServer"/> confines serving
/// and validates the opened source against its root with <see cref="LaunchRoot"/>).
/// </summary>
internal static class DirectoryBrowser
{
    /// <summary>
    /// Lists <paramref name="directory"/>, or <c>null</c> when it is not a directory. An
    /// unreadable directory lists as empty rather than failing the request.
    /// </summary>
    public static BrowseListing? List(string directory)
    {
        var full = Path.GetFullPath(directory);
        if (!Directory.Exists(full))
        {
            return null;
        }

        var parent = Directory.GetParent(full)?.FullName;
        try
        {
            var directories = Directory.EnumerateDirectories(full)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            var sources = Directory.EnumerateFiles(full, $"*{DocumentValidation.Extension}")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            return new BrowseListing(full, parent, directories, sources);
        }
        catch (UnauthorizedAccessException)
        {
            return new BrowseListing(full, parent, [], []);
        }
    }
}
