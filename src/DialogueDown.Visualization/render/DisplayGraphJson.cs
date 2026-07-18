using System.Text.Json;
using System.Text.Json.Serialization;
using DialogueDown.Visualization.Configuration;

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
    /// <paramref name="symbols"/> (omitted when null), and the applied
    /// <paramref name="configuration"/> for the Config tab (omitted when null).
    /// </summary>
    public static string SerializeReport(
        string mode,
        string? path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null) =>
        JsonSerializer.Serialize(
            new { mode, path, source, stages, symbols, configuration }, _options);

    /// <summary>
    /// Serializes the current document payload —
    /// <c>{ mode, path, source, stages, symbols, configuration }</c> — for the live server's
    /// document API and its hot-reload push events.
    /// </summary>
    public static string SerializeDocument(
        string mode,
        string path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null) =>
        JsonSerializer.Serialize(
            new { mode, path, source, stages, symbols, configuration }, _options);
}
