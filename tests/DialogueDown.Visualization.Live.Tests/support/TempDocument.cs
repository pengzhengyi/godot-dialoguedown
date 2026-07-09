namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>A temporary <c>.dialogue.md</c> file that deletes itself on dispose.</summary>
internal sealed class TempDocument : IDisposable
{
    public TempDocument(string content = "# Scene\n\nAlice: Hi.")
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"dd-test-{Guid.NewGuid():N}.dialogue.md");
        File.WriteAllText(Path, content);
    }

    /// <summary>The absolute path of the temporary document.</summary>
    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
