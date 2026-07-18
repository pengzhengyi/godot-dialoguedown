using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the visualization run modes for the <c>dialoguedown visualize</c> command:
/// a one-shot static export, or a served session that hosts the View/Edit toggle and
/// runs until canceled. Injected so the command is testable with a substitute.
/// </summary>
public interface IVisualizeRunner
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report and opens it (unless
    /// <paramref name="noOpen"/>), or writes it to <paramref name="output"/>, showing the
    /// applied <paramref name="configuration"/> in the Config tab. Returns a process exit code.
    /// </summary>
    int RunStatic(string file, string? output, bool noOpen, AppliedConfiguration configuration);

    /// <summary>
    /// Renders every stage of <paramref name="file"/> as text in the given
    /// <paramref name="format"/> (Mermaid or DOT), using the project's
    /// <paramref name="options"/>, and writes it to <paramref name="output"/>, or to
    /// standard output when null. A non-interactive emit — no server, no browser.
    /// Returns a process exit code.
    /// </summary>
    int RunEmit(string file, EmitFormat format, string? output, CompilerOptions options);

    /// <summary>
    /// Serves an interactive report for <paramref name="file"/>, showing the applied
    /// <paramref name="configuration"/> in the Config tab, on a loopback port and keeps it up
    /// until <paramref name="cancellationToken"/> is canceled. The reader toggles View/Edit in
    /// the browser; <paramref name="mode"/> is the initial side
    /// (<see cref="VisualizationMode.View"/> or <see cref="VisualizationMode.Edit"/>).
    /// <paramref name="renderRoot"/> pins the static-asset root (otherwise it is resolved,
    /// with consent, from the document's referenced images). Returns a process exit code.
    /// </summary>
    Task<int> RunServedAsync(
        string file, int? port, bool noOpen, string? renderRoot, string mode,
        AppliedConfiguration configuration, CancellationToken cancellationToken);
}
