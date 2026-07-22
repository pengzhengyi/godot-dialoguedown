namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>Test helper for creating symbolic links (available on the Linux CI and Unix dev hosts).</summary>
internal static class Symlinks
{
    /// <summary>Creates the symbolic link <paramref name="link"/> pointing at <paramref name="target"/>.</summary>
    public static void Create(string link, string target) => File.CreateSymbolicLink(link, target);
}
