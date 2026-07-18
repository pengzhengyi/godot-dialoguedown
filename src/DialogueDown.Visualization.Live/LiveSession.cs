using System.Text.Json;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A live session bound to one document. It reads and compiles the current file on
/// demand (for the initial page and the document API) and pushes hot-reload events
/// to connected clients; the watcher calls <see cref="Refresh"/> when the file
/// changes on disk. When the compile applied a <c>dialogue.toml</c>, the session can
/// also save an edited configuration (<see cref="SaveConfig"/>) and recompile with it;
/// a session with no configuration file can create one (<see cref="CreateConfig"/>).
/// </summary>
internal sealed class LiveSession
{
    private string? _configPath;
    private CompilationVisualizer _visualizer;
    private volatile string? _lastSaved;

    /// <summary>Creates a session for <paramref name="documentPath"/> in the given <paramref name="mode"/>.</summary>
    public LiveSession(
        string documentPath,
        string mode = VisualizationMode.View,
        CompilationVisualizer? visualizer = null,
        string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        DocumentPath = documentPath;
        Mode = mode;
        _visualizer = visualizer ?? new CompilationVisualizer();
        _configPath = configPath;
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
    /// Writes <paramref name="source"/> to the configuration file (a force overwrite),
    /// re-parses it, and recompiles the document with the new configuration — so the graphs
    /// and the configured speakers both reflect the edit. Returns the recompiled document
    /// payload. Malformed TOML throws (like a broken document save): the file is written,
    /// then the parse surfaces the problem. Throws <see cref="InvalidOperationException"/>
    /// when the session has no configuration file to save.
    /// </summary>
    public string SaveConfig(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to save.");
        }

        return ApplyConfig(_configPath, source);
    }

    /// <summary>
    /// Creates a <c>dialogue.toml</c> at <paramref name="configPath"/> for a session that has
    /// none, seeds it with the <see cref="ConfigStarter.Template">starter template</see>, adopts
    /// it (so later <see cref="SaveConfig"/> calls and reloads apply it), and returns the
    /// recompiled document payload. Throws <see cref="InvalidOperationException"/> when the
    /// session already has a configuration file; the caller guards against overwriting an
    /// existing file on disk (see the server's create route).
    /// </summary>
    public string CreateConfig(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        if (_configPath is not null)
        {
            throw new InvalidOperationException("This session already has a configuration file.");
        }

        _configPath = configPath;
        return ApplyConfig(configPath, ConfigStarter.Template);
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

    // Writes source to the configuration file, re-parses it, rebuilds the visualizer with the
    // parsed options, and returns the recompiled document payload — shared by SaveConfig (an
    // edit of an existing file) and CreateConfig (a new file seeded with the starter template).
    // Malformed TOML throws after the write, exactly as an edit would.
    private string ApplyConfig(string configPath, string source)
    {
        File.WriteAllText(configPath, source);
        var options = TomlConfigurationLoader.Parse(source, configPath);
        _visualizer = new CompilationVisualizer(
            AppliedConfiguration.FromFile(configPath, source, options));
        return _visualizer.SerializeDocument(DocumentPath, File.ReadAllText(DocumentPath), Mode);
    }
}
