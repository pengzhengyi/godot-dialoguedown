using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// The stable cross-link key for a <see cref="SpeakerSymbol"/> entity: its <c>@id</c> when it
/// has one, else its name, else the anonymous default — prefixed <c>speaker:</c>. Shared by the
/// speaker's rows so a name and an id for one speaker resolve to one highlight.
/// </summary>
internal static class SpeakerEntity
{
    /// <summary>The cross-link key for a speaker, for example <c>speaker:@guide</c>.</summary>
    public static string Key(SpeakerSymbol speaker)
    {
        // Key by identity — id, then name — so a named speaker that is also the default keys
        // by its identity, not "(default)". Only the anonymous default has neither.
        var identity = speaker.Id is not null ? $"@{speaker.Id}" : speaker.Name ?? "(default)";
        return $"speaker:{identity}";
    }
}
