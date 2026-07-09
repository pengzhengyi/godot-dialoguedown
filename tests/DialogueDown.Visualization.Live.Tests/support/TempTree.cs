namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>
/// A temporary directory tree that deletes itself on dispose. Used to lay out a
/// document and its assets in known folders (siblings, nested, outside) for
/// serve-root tests.
/// </summary>
internal sealed class TempTree : IDisposable
{
    public TempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), $"dd-tree-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>The absolute path of the tree's root directory.</summary>
    public string Root { get; }

    /// <summary>Creates (if needed) the subdirectory at <paramref name="relative"/> and returns its absolute path.</summary>
    public string Dir(string relative)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Writes a file at <paramref name="relative"/> (creating parents) and returns its absolute path.</summary>
    public string File(string relative, string content = "")
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
