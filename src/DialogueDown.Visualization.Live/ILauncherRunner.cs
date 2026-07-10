namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the launcher for the <c>dialoguedown visualize</c> command when the command is
/// not fully specified: it serves the launcher page from a loopback server so a source,
/// mode, and root can be chosen in the browser, and stays up until canceled. Injected so
/// the command is testable with a substitute.
/// </summary>
public interface ILauncherRunner
{
    /// <summary>
    /// Serves the launcher rooted at <paramref name="root"/> on a loopback port, pre-filled
    /// with an optional root-relative <paramref name="source"/> and a <paramref name="mode"/>,
    /// opening the browser unless <paramref name="noOpen"/>. Runs until
    /// <paramref name="cancellationToken"/> is canceled. Returns a process exit code.
    /// </summary>
    Task<int> RunAsync(
        string root,
        string? source,
        LaunchMode mode,
        int? port,
        bool noOpen,
        CancellationToken cancellationToken);
}
