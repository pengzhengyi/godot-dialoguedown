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
    private volatile string? _lastSaved;

    /// <summary>Creates a session for <paramref name="documentPath"/> in the given <paramref name="mode"/>.</summary>
    public LiveSession(
        string documentPath,
        string mode = VisualizationMode.View,
        CompilationVisualizer? visualizer = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        DocumentPath = documentPath;
        Mode = mode;
        _visualizer = visualizer ?? new CompilationVisualizer();
    }

    /// <summary>The absolute path of the document this session serves.</summary>
    public string DocumentPath { get; }

    /// <summary>The mode this session serves in (watch or live).</summary>
    public string Mode { get; }

    /// <summary>The event stream shared by every connected client.</summary>
    public SseBroadcaster Broadcaster { get; } = new();

    /// <summary>Renders the initial live report HTML for the current file.</summary>
    public string RenderInitialHtml() =>
        _visualizer.RenderLiveReport(DocumentPath, File.ReadAllText(DocumentPath), Mode);

    /// <summary>Serializes the current document payload (<c>{ mode, path, source, stages }</c>).</summary>
    public string CurrentDocumentJson() =>
        _visualizer.SerializeDocument(DocumentPath, File.ReadAllText(DocumentPath), Mode);

    /// <summary>
    /// Writes <paramref name="source"/> to the document file (a force overwrite),
    /// recompiles it, and returns the document payload. Records the written content so
    /// the watcher's self-triggered <see cref="Refresh"/> is not mistaken for an external
    /// edit (see <see cref="Refresh"/>).
    /// </summary>
    public string Save(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // Record before writing so the watcher, even if it fires immediately, can tell
        // this write from an external one.
        _lastSaved = source;
        File.WriteAllText(DocumentPath, source);
        return _visualizer.SerializeDocument(DocumentPath, source, Mode);
    }

    /// <summary>
    /// Recompiles the current file and pushes a <c>reload</c> to every client, or a
    /// <c>problem</c> event carrying an error message when the file is missing or
    /// cannot be read. A change whose content matches the last <see cref="Save"/> is the
    /// browser's own write and is skipped, so a save does not bounce back as a reload.
    /// </summary>
    public void Refresh()
    {
        try
        {
            var current = File.ReadAllText(DocumentPath);
            if (current == _lastSaved)
            {
                return;
            }

            Broadcaster.Broadcast(
                new LiveEvent("reload", _visualizer.SerializeDocument(DocumentPath, current, Mode)));
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
