using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Diagnostics;

namespace DialogueDown.Visualization;

/// <summary>
/// Serializes the report data to the JSON the client script consumes. The default
/// encoder escapes HTML-sensitive characters (<c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>),
/// so the JSON is safe to inline inside a <c>&lt;script&gt;</c> block — a label or
/// source containing <c>&lt;/script&gt;</c> cannot break out of the tag.
/// </summary>
internal static class DisplayGraphJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(IEnumerable<DisplayGraph> graphs) =>
        JsonSerializer.Serialize(graphs, _options);

    /// <summary>
    /// Serializes the report payload injected into the page — the display
    /// <paramref name="mode"/> (static/watch/live), the document <paramref name="path"/>
    /// when known, the compiled <paramref name="source"/> (shown in the Source tab;
    /// omitted when null), each stage's display graph, the editor's resolved
    /// <paramref name="symbols"/> (omitted when null), the applied
    /// <paramref name="configuration"/> for the Config tab (omitted when null), and the
    /// LSP-shaped <paramref name="diagnostics"/> the editor overlay renders (omitted when
    /// null; an empty array clears the overlay after a clean compile).
    /// </summary>
    public static string SerializeReport(
        string mode,
        string? path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        ConfigStatusOverlay? configOverlay = null)
    {
        var json = JsonSerializer.Serialize(
            new { mode, path, source, stages, symbols, configuration, diagnostics }, _options);
        return configOverlay is null ? json : ApplyConfigOverlay(json, configOverlay);
    }

    /// <summary>
    /// Serializes the current document payload —
    /// <c>{ mode, path, source, stages, symbols, configuration, diagnostics }</c> — for the live
    /// server's document API and its hot-reload push events.
    /// </summary>
    public static string SerializeDocument(
        string mode,
        string path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        ConfigStatusOverlay? configOverlay = null)
    {
        var json = JsonSerializer.Serialize(
            new { mode, path, source, stages, symbols, configuration, diagnostics }, _options);
        return configOverlay is null ? json : ApplyConfigOverlay(json, configOverlay);
    }

    // A saved-invalid Config overlay: the graphs and speakers stay the last valid compile, but the
    // Config tab must show the current invalid source and the payload must announce it is stale, so
    // a reload of the page restores the saved-invalid state instead of the last valid text.
    private static string ApplyConfigOverlay(string json, ConfigStatusOverlay overlay)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        node["configStatus"] = "saved-invalid";
        node["configMessage"] = overlay.Message;
        if (node["configuration"] is JsonObject configuration
            && configuration["file"] is JsonObject file)
        {
            file["source"] = overlay.Source;
        }

        return node.ToJsonString();
    }
}
