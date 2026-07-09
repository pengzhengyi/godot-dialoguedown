namespace DialogueDown.Cli.Tests.Support;

/// <summary>A temporary <c>.dialogue.md</c> script that deletes itself on dispose.</summary>
internal sealed class TempScript : IDisposable
{
    public TempScript(string content = "# Scene")
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"dd-cli-{Guid.NewGuid():N}.dialogue.md");
        File.WriteAllText(Path, content);
    }

    /// <summary>The absolute path of the temporary script.</summary>
    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
