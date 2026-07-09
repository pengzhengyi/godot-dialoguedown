using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Serializes the whole report — the compiled <paramref name="source"/> (shown
    /// in the Source tab; omitted when null) and each stage's display graph.
    /// </summary>
    public static string SerializeReport(string? source, IEnumerable<DisplayGraph> stages) =>
        JsonSerializer.Serialize(new { source, stages }, _options);
}
