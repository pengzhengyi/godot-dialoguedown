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
    private volatile string? _lastSavedConfig;

    // The configuration currently persisted on disk, tracked apart from the last valid visualizer:
    // a saved-invalid Config keeps the last valid report but persists invalid TOML, so a page
    // reload must restore that saved-invalid state rather than the last valid text.
    private string? _currentConfigSource;
    private bool _currentConfigValid = true;
    private string _currentConfigError = string.Empty;

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

    /// <summary>
    /// The absolute path of the configuration file this session applies, or <c>null</c> when the
    /// document compiles on the built-in defaults. It becomes non-null once a config is created or
    /// adopted (see <see cref="CreateConfig"/>), so the caller can start watching it for external
    /// changes.
    /// </summary>
    public string? ConfigPath => _configPath;

    /// <summary>The event stream shared by every connected client.</summary>
    public SseBroadcaster Broadcaster { get; } = new();

    /// <summary>Renders the initial live report HTML for the current file.</summary>
    public string RenderInitialHtml() =>
        _visualizer.RenderLiveReport(
            DocumentPath, File.ReadAllText(DocumentPath), Mode, CurrentConfigOverlay());

    /// <summary>Serializes the current document payload (<c>{ mode, path, source, stages }</c>).</summary>
    public string CurrentDocumentJson() =>
        _visualizer.SerializeDocument(
            DocumentPath, File.ReadAllText(DocumentPath), Mode, CurrentConfigOverlay());

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
    /// Creates a <c>dialogue.toml</c> at <paramref name="configPath"/> for a session that has none
    /// and adopts it (so later saves and reloads apply it). The write is exclusive
    /// (<see cref="FileMode.CreateNew"/>): it either creates the starter file
    /// (<see cref="CreateConfigStatus.Created"/>), or — when a file already exists — adopts it
    /// idempotently if it equals the starter template (<see cref="CreateConfigStatus.Adopted"/>, a
    /// create retry after a lost response) and reports a <see cref="CreateConfigStatus.Conflict"/>
    /// otherwise, writing nothing. <see cref="ConfigPath"/> is assigned only after a successful
    /// creation or adoption, so a failed create leaves the no-config state unchanged for a retry.
    /// A retry that arrives after this session already adopted the same <paramref name="configPath"/>
    /// (its first response was lost) is idempotent while the file is still the untouched starter
    /// template and a <see cref="CreateConfigStatus.Conflict"/> once the content diverges. Throws
    /// <see cref="InvalidOperationException"/> when the session already has a <em>different</em>
    /// configuration file.
    /// </summary>
    public CreateConfigResult CreateConfig(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        if (_configPath is not null)
        {
            if (!PathsEqual(configPath, _configPath))
            {
                throw new InvalidOperationException("This session already has a configuration file.");
            }

            // A lost-response retry of the create that this session already satisfied: idempotent
            // while the file is still the starter template, a conflict once its content diverges.
            return AdoptOrConflict(configPath);
        }

        try
        {
            AtomicFile.CreateNew(configPath, ConfigStarter.Template);
        }
        catch (IOException) when (File.Exists(configPath))
        {
            return AdoptOrConflict(configPath);
        }

        return new CreateConfigResult(
            CreateConfigStatus.Created, AdoptConfig(configPath, ConfigStarter.Template));
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
            // One-shot suppression: a self-write arms a single suppression token, consumed here so a
            // later external change back to the same content still reloads (an A->B->A sequence).
            var expected = _lastSaved;
            _lastSaved = null;
            if (current == expected)
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
            Broadcaster.Broadcast(new LiveEvent("problem", ProblemJson(message, "document")));
        }
    }

    /// <summary>
    /// Recompiles with the configuration's current on-disk content and pushes a
    /// <c>reload-config</c> to every client (a <c>problem</c> event when the file is missing or
    /// cannot be read). A valid external config is adopted so View stays consistent for later
    /// recompiles; an invalid one keeps the last valid report but carries the external TOML.
    /// A change whose content matches the last config write is the browser's own write and is
    /// skipped, so a config save does not bounce back as a reload. Does nothing when the session
    /// has no configuration file.
    /// </summary>
    public void RefreshConfig()
    {
        if (_configPath is null)
        {
            return;
        }

        try
        {
            var current = File.ReadAllText(_configPath);
            // One-shot suppression: a self-write arms a single suppression token, consumed here so a
            // later external change back to the same content still reloads (an A->B->A sequence).
            var expected = _lastSavedConfig;
            _lastSavedConfig = null;
            if (current == expected)
            {
                return;
            }

            Broadcaster.Broadcast(new LiveEvent("reload-config", ConfigReloadPayload(current)));
        }
        catch (IOException ex)
        {
            var message = ex is FileNotFoundException or DirectoryNotFoundException
                ? $"Configuration not found: {_configPath}"
                : ex.Message;
            Broadcaster.Broadcast(new LiveEvent("problem", ProblemJson(message, "config")));
        }
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

    // A missing-file/read-failure problem carries which document it is about (the served
    // document or its configuration) so the client can route it through that controller's
    // disk-change/conflict path instead of only flashing a banner.
    private static string ProblemJson(string message, string target) =>
        JsonSerializer.Serialize(new { message, target });

    // Two paths name the same file — compared as normalized full paths so a session recognizes a
    // create retry for the very config it already adopted.
    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.Ordinal);

    // An exclusive create lost the race to an existing file: adopt it idempotently when it equals
    // the starter template (a create retry after a lost response), otherwise report a conflict and
    // leave it untouched.
    private CreateConfigResult AdoptOrConflict(string configPath)
    {
        var existing = File.ReadAllText(configPath);
        if (!string.Equals(existing, ConfigStarter.Template, StringComparison.Ordinal))
        {
            return new CreateConfigResult(
                CreateConfigStatus.Conflict, "A dialogue.toml already exists — reload to edit it.");
        }

        return new CreateConfigResult(CreateConfigStatus.Adopted, AdoptConfig(configPath, existing));
    }

    // Parses and adopts an on-disk config (already written) without rewriting it, assigning
    // _configPath only after the parse succeeds and recording it as the last self-write so the
    // config watcher's own event is suppressed. Returns the recompiled document payload.
    private string AdoptConfig(string configPath, string source)
    {
        var options = TomlConfigurationLoader.Parse(source, configPath);
        _configPath = configPath;
        AdoptValidConfig(source, options);
        _lastSavedConfig = source;
        return WithOutcome(SerializeCurrent(), "saved");
    }

    // Builds the payload for an external config change: a valid config is adopted into the
    // visualizer and returned as `loaded`; an invalid one keeps the last valid report and carries
    // the external (invalid) TOML as `invalid` so the editor can show it.
    private string ConfigReloadPayload(string source)
    {
        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "loaded");
        }

        RecordInvalidConfig(source, error);
        return WithConfigSource(SerializeCurrent(), source, "invalid", error);
    }

    // Adopts a valid config into the visualizer and records it as the current, valid on-disk
    // configuration so a page reload renders it without a stale overlay.
    private void AdoptValidConfig(string source, CompilerOptions options)
    {
        _visualizer = new CompilationVisualizer(
            AppliedConfiguration.FromFile(_configPath!, source, options));
        _currentConfigSource = source;
        _currentConfigValid = true;
        _currentConfigError = string.Empty;
    }

    // Records the current on-disk config as persisted-but-invalid so a page reload restores the
    // saved-invalid state (the invalid source and a stale report) instead of the last valid text.
    private void RecordInvalidConfig(string source, string error)
    {
        _currentConfigSource = source;
        _currentConfigValid = false;
        _currentConfigError = error;
    }

    // The overlay a served page needs when the persisted config is invalid: none while it is valid.
    private ConfigStatusOverlay? CurrentConfigOverlay() =>
        _currentConfigValid
            ? null
            : new ConfigStatusOverlay(_currentConfigSource ?? string.Empty, _currentConfigError);

    private string SaveDocument(SaveInput input)
    {
        var source = input.Source ?? string.Empty;
        try
        {
            return AtomicFile.Transact(DocumentPath, transaction =>
            {
                var disk = transaction.Disk;
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

                    _lastSaved = source;
                    transaction.Write(source); // compare-and-swap: an external write in the window conflicts
                }
                else
                {
                    _lastSaved = source;
                    transaction.WriteForced(source); // confirmed overwrite
                }

                return WithOutcome(_visualizer.SerializeDocument(DocumentPath, source, Mode), "saved");
            });
        }
        catch (AtomicFile.WriteConflictException)
        {
            return OutcomeJson("conflict", "The document changed on disk.");
        }
    }

    private string SaveConfig(SaveInput input)
    {
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to save.");
        }

        var source = input.Source ?? string.Empty;
        try
        {
            return AtomicFile.Transact(_configPath, transaction =>
            {
                var disk = transaction.Disk;
                if (!input.Overwrite)
                {
                    if (disk == source)
                    {
                        return RecompileConfig(source, transaction, write: false, force: false); // idempotent recovery
                    }

                    if (disk != input.ExpectedBaseline)
                    {
                        return OutcomeJson("conflict", "The configuration changed on disk.");
                    }
                }

                if (input.RequireValid && !TryParseConfig(source, out _, out var validationError))
                {
                    // Auto/navigation never writes invalid TOML; the exclusive handle is released unwritten.
                    return OutcomeJson("invalid-auto", validationError);
                }

                return RecompileConfig(source, transaction, write: true, force: input.Overwrite);
            });
        }
        catch (AtomicFile.WriteConflictException)
        {
            return OutcomeJson("conflict", "The configuration changed on disk.");
        }
    }

    // Writes source through the held transaction (unless it already equals disk), then compiles:
    // valid TOML is adopted and returned as `saved`; invalid TOML keeps the last valid report and
    // returns `saved-invalid` carrying the persisted (invalid) source so the editor can show it.
    private string RecompileConfig(string source, AtomicFile.Transaction transaction, bool write, bool force)
    {
        // After this call the disk equals `source`, so record it as the last self-write to
        // suppress the config watcher's own event (see RefreshConfig).
        _lastSavedConfig = source;
        if (write)
        {
            if (force)
            {
                transaction.WriteForced(source); // confirmed overwrite
            }
            else
            {
                transaction.Write(source); // compare-and-swap against the snapshot
            }
        }

        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "saved");
        }

        RecordInvalidConfig(source, error);
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
        _lastSavedConfig = source;
        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "loaded");
        }

        RecordInvalidConfig(source, error);
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
}
