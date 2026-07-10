namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the visualization run modes for the <c>dialoguedown visualize</c> command:
/// a one-shot static render, a watch session that hot-reloads the report, or a Live
/// Edit session that serves an editable report — the last two run until canceled.
/// Injected so the command is testable with a substitute.
/// </summary>
public interface IVisualizeRunner
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report and opens it (unless
    /// <paramref name="noOpen"/>), or writes it to <paramref name="output"/>. Returns a
    /// process exit code.
    /// </summary>
    int RunStatic(string file, string? output, bool noOpen);

    /// <summary>
    /// Serves the live report for <paramref name="file"/> on a loopback port and
    /// hot-reloads it until <paramref name="cancellationToken"/> is canceled.
    /// <paramref name="renderRoot"/> pins the static-asset root (otherwise it is
    /// resolved, with consent, from the document's referenced images). Returns a
    /// process exit code.
    /// </summary>
    Task<int> RunWatchAsync(
        string file, int? port, bool noOpen, string? renderRoot, CancellationToken cancellationToken);

    /// <summary>
    /// Serves an <b>editable</b> live report for <paramref name="file"/> on a loopback
    /// port (Live Edit) and keeps it up until <paramref name="cancellationToken"/> is
    /// canceled. <paramref name="renderRoot"/> pins the static-asset root as in
    /// <see cref="RunWatchAsync"/>. Returns a process exit code.
    /// </summary>
    Task<int> RunLiveAsync(
        string file, int? port, bool noOpen, string? renderRoot, CancellationToken cancellationToken);
}
