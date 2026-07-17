namespace DialogueDown.Cli.Tests.Support;

/// <summary>A temporary directory tree that deletes itself on dispose, for config-discovery tests.</summary>
internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dd-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    /// <summary>The absolute path of the temporary directory root.</summary>
    public string Path { get; }

    /// <summary>Creates a subdirectory (nested paths allowed) and returns its absolute path.</summary>
    public string Dir(string relative)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(full);
        return full;
    }

    /// <summary>Writes a file under the directory (creating parents) and returns its absolute path.</summary>
    public string Write(string relative, string content)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
