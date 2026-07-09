using Timer = System.Timers.Timer;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Coalesces a burst of rapid triggers into a single action, fired once the
/// triggers go quiet for the debounce delay. Editors save by writing several times
/// (or writing a temp file and renaming), so the watcher debounces those bursts
/// into one recompile.
/// </summary>
internal sealed class Debouncer : IDisposable
{
    private readonly Timer _timer;

    /// <summary>Fires <paramref name="action"/> once triggers stay quiet for <paramref name="delay"/>.</summary>
    public Debouncer(TimeSpan delay, Action action)
    {
        _timer = new Timer(delay.TotalMilliseconds) { AutoReset = false };
        _timer.Elapsed += (_, _) => action();
    }

    /// <summary>Registers activity; restarts the quiet-period countdown.</summary>
    public void Trigger()
    {
        _timer.Stop();
        _timer.Start();
    }

    /// <inheritdoc />
    public void Dispose() => _timer.Dispose();
}
