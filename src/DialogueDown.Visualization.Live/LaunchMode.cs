namespace DialogueDown.Visualization.Live;

/// <summary>
/// How the launcher opens a source: served once (<see cref="Static"/>), watched for
/// hot reload (<see cref="Watch"/>), or live-edited (<see cref="Live"/>, reserved for
/// Component 2 and not yet functional).
/// </summary>
public enum LaunchMode
{
    /// <summary>Render once and serve; no watcher.</summary>
    Static,

    /// <summary>Serve and hot-reload when the source changes on disk.</summary>
    Watch,

    /// <summary>In-browser live editing (Component 2; accepted but not yet functional).</summary>
    Live,
}
