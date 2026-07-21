using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A live session bound to one document. It reads and compiles the current file on
/// demand (for the initial page and the document API) and pushes hot-reload events
/// to connected clients; the watcher calls <see cref="Refresh"/> when the file
/// changes on disk. Saving is optimistic and generation-safe: a write carries the
/// baseline it expects on disk (<see cref="Save"/>), so an external change is reported
/// as a conflict instead of being overwritten silently, and Config Auto validates the
/// TOML before writing. When the compile applied a <c>dialogue.toml</c>, the session
/// can also save an edited configuration and recompile with it; a session with no
/// configuration file can create one (<see cref="CreateConfig"/>).
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
    /// Applies one save request and returns a payload carrying a typed <c>outcome</c>:
    /// <c>saved</c>, <c>saved-invalid</c>, <c>invalid-auto</c>, or <c>conflict</c>. A dialogue
    /// save is always written (its errors surface as diagnostics); a Config save validates when
    /// the request requires it, and every non-forced write first compares the disk against the
    /// requested source (idempotent recovery) and the expected baseline (conflict detection).
    /// Records the written content so the watcher's self-triggered <see cref="Refresh"/> is not
    /// mistaken for an external edit.
    /// </summary>
    public string Save(SaveInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.IsConfig ? SaveConfig(input) : SaveDocument(input);
    }

    /// <summary>
    /// Reloads the document (or its configuration, when <paramref name="target"/> is
    /// <c>"config"</c>) from disk for a Reload from a conflict/uncertain state, returning a
    /// payload with a typed <c>outcome</c> (<c>loaded</c>, <c>invalid</c>, or <c>missing</c>).
    /// A valid Config reload adopts the external file; an invalid one keeps the last valid
    /// report and carries the external TOML so the editor can show it.
    /// </summary>
    public string Reload(string? target)
    {
        var isConfig = string.Equals(target, "config", StringComparison.OrdinalIgnoreCase);
        return isConfig ? ReloadConfig() : ReloadDocument();
    }

    /// <summary>
    /// Creates a <c>dialogue.toml</c> at <paramref name="configPath"/> for a session that has
    /// none, seeds it with the <see cref="ConfigStarter.Template">starter template</see>, adopts
    /// it (so later saves and reloads apply it), and returns the recompiled document payload.
    /// Throws <see cref="InvalidOperationException"/> when the session already has a configuration
    /// file; the caller guards against overwriting an existing file on disk (see the server's
    /// create route).
    /// </summary>
    public string CreateConfig(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        if (_configPath is not null)
        {
            throw new InvalidOperationException("This session already has a configuration file.");
        }

        _configPath = configPath;
        return WithOutcome(ApplyConfig(configPath, ConfigStarter.Template), "saved");
    }

    /// <summary>
    /// Adopts an existing <c>dialogue.toml</c> at <paramref name="configPath"/> (whose content
    /// already equals the starter template) without rewriting it, so a create retry after a lost
    /// response recovers idempotently. Returns the recompiled document payload.
    /// </summary>
    public string AdoptExistingConfig(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        var source = File.ReadAllText(configPath);
        var options = TomlConfigurationLoader.Parse(source, configPath);
        _configPath = configPath;
        _visualizer = new CompilationVisualizer(
            AppliedConfiguration.FromFile(configPath, source, options));
        return WithOutcome(SerializeCurrent(), "saved");
    }

    /// <summary>
    /// Recompiles the current file and pushes a <c>reload</c> to every client, or a
    /// <c>problem</c> event carrying an error message when the file is missing or
    /// cannot be read. A change whose content matches the last write is the browser's own write
    /// and is skipped, so a save does not bounce back as a reload.
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

    private string SaveDocument(SaveInput input)
    {
        var source = input.Source ?? string.Empty;
        var disk = File.Exists(DocumentPath) ? File.ReadAllText(DocumentPath) : null;
        if (!input.Overwrite)
        {
            if (disk == source)
            {
                _lastSaved = source; // idempotent: the disk already equals the requested source
                return WithOutcome(_visualizer.SerializeDocument(DocumentPath, source, Mode), "saved");
            }

            if (disk != input.ExpectedBaseline)
            {
                return OutcomeJson("conflict", "The document changed on disk.");
            }
        }

        _lastSaved = source;
        File.WriteAllText(DocumentPath, source);
        return WithOutcome(_visualizer.SerializeDocument(DocumentPath, source, Mode), "saved");
    }

    private string SaveConfig(SaveInput input)
    {
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to save.");
        }

        var source = input.Source ?? string.Empty;
        var disk = File.Exists(_configPath) ? File.ReadAllText(_configPath) : null;
        if (!input.Overwrite)
        {
            if (disk == source)
            {
                return RecompileConfig(source, write: false); // idempotent recovery
            }

            if (disk != input.ExpectedBaseline)
            {
                return OutcomeJson("conflict", "The configuration changed on disk.");
            }
        }

        if (input.RequireValid && !TryParseConfig(source, out _, out var validationError))
        {
            return OutcomeJson("invalid-auto", validationError); // Auto/navigation never writes invalid TOML
        }

        return RecompileConfig(source, write: true);
    }

    // Writes source (unless it already equals disk), then compiles: valid TOML is adopted and
    // returned as `saved`; invalid TOML keeps the last valid report and returns `saved-invalid`
    // carrying the persisted (invalid) source so the editor can show it.
    private string RecompileConfig(string source, bool write)
    {
        if (write)
        {
            File.WriteAllText(_configPath!, source);
        }

        if (TryParseConfig(source, out var options, out var error))
        {
            _visualizer = new CompilationVisualizer(
                AppliedConfiguration.FromFile(_configPath!, source, options!));
            return WithOutcome(SerializeCurrent(), "saved");
        }

        return WithConfigSource(SerializeCurrent(), source, "saved-invalid", error);
    }

    private string ReloadDocument()
    {
        if (!File.Exists(DocumentPath))
        {
            return OutcomeJson("missing", "The document was deleted on disk.");
        }

        var current = File.ReadAllText(DocumentPath);
        _lastSaved = current;
        return WithOutcome(_visualizer.SerializeDocument(DocumentPath, current, Mode), "loaded");
    }

    private string ReloadConfig()
    {
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to reload.");
        }

        if (!File.Exists(_configPath))
        {
            return OutcomeJson("missing", "The configuration was deleted on disk.");
        }

        var source = File.ReadAllText(_configPath);
        if (TryParseConfig(source, out var options, out var error))
        {
            _visualizer = new CompilationVisualizer(
                AppliedConfiguration.FromFile(_configPath, source, options!));
            return WithOutcome(SerializeCurrent(), "loaded");
        }

        return WithConfigSource(SerializeCurrent(), source, "invalid", error);
    }

    private string SerializeCurrent() =>
        _visualizer.SerializeDocument(DocumentPath, File.ReadAllText(DocumentPath), Mode);

    private bool TryParseConfig(string source, out CompilerOptions? options, out string error)
    {
        try
        {
            options = TomlConfigurationLoader.Parse(source, _configPath!);
            error = string.Empty;
            return true;
        }
        catch (DialogueConfigurationException ex)
        {
            options = null;
            error = ex.Message;
            return false;
        }
    }

    private string ApplyConfig(string configPath, string source)
    {
        File.WriteAllText(configPath, source);
        var options = TomlConfigurationLoader.Parse(source, configPath);
        _visualizer = new CompilationVisualizer(
            AppliedConfiguration.FromFile(configPath, source, options));
        return _visualizer.SerializeDocument(DocumentPath, File.ReadAllText(DocumentPath), Mode);
    }

    private static string WithOutcome(string payloadJson, string outcome)
    {
        var node = JsonNode.Parse(payloadJson)!.AsObject();
        node["outcome"] = outcome;
        return node.ToJsonString();
    }

    // A saved-invalid/invalid Config payload keeps the last valid report but must carry the
    // external (invalid) TOML so the editor can show it — inject it into configuration.file.source.
    private static string WithConfigSource(string payloadJson, string configSource, string outcome, string message)
    {
        var node = JsonNode.Parse(payloadJson)!.AsObject();
        node["outcome"] = outcome;
        node["message"] = message;
        if (node["configuration"] is JsonObject configuration
            && configuration["file"] is JsonObject file)
        {
            file["source"] = configSource;
        }

        return node.ToJsonString();
    }

    private static string OutcomeJson(string outcome, string message) =>
        JsonSerializer.Serialize(new { outcome, message });

    private static string ProblemJson(string message) =>
        JsonSerializer.Serialize(new { message });
}
