using System.Text.Json;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A live session bound to one document. It reads and compiles the current file on
/// demand (for the initial page and the document API) and pushes hot-reload events
/// to connected clients; the watcher calls <see cref="Refresh"/> when the file
/// changes on disk.
/// </summary>
internal sealed class LiveSession
{
    private readonly CompilationVisualizer _visualizer;

    /// <summary>Creates a session for <paramref name="documentPath"/>.</summary>
    public LiveSession(string documentPath, CompilationVisualizer? visualizer = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        DocumentPath = documentPath;
        _visualizer = visualizer ?? new CompilationVisualizer();
    }

    /// <summary>The absolute path of the document this session serves.</summary>
    public string DocumentPath { get; }

    /// <summary>The event stream shared by every connected client.</summary>
    public SseBroadcaster Broadcaster { get; } = new();

    /// <summary>Renders the initial live report HTML for the current file.</summary>
    public string RenderInitialHtml() =>
        _visualizer.RenderLiveReport(DocumentPath, File.ReadAllText(DocumentPath));

    /// <summary>Serializes the current document payload (<c>{ path, source, stages }</c>).</summary>
    public string CurrentDocumentJson() =>
        _visualizer.SerializeDocument(DocumentPath, File.ReadAllText(DocumentPath));

    /// <summary>
    /// Recompiles the current file and pushes a <c>reload</c> to every client, or a
    /// <c>problem</c> event carrying an error message when the file is missing or
    /// cannot be read.
    /// </summary>
    public void Refresh()
    {
        try
        {
            Broadcaster.Broadcast(new LiveEvent("reload", CurrentDocumentJson()));
        }
        catch (IOException ex)
        {
            var message = ex is FileNotFoundException or DirectoryNotFoundException
                ? $"Document not found: {DocumentPath}"
                : ex.Message;
            Broadcaster.Broadcast(new LiveEvent("problem", ProblemJson(message)));
        }
    }

    private static string ProblemJson(string message) =>
        JsonSerializer.Serialize(new { message });
}
