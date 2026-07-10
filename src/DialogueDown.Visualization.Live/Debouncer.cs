namespace DialogueDown.Visualization.Live;

/// <summary>
/// Coalesces a burst of rapid triggers into a single action, fired once the
/// triggers go quiet for the debounce delay. Editors save by writing several times
/// (or writing a temp file and renaming), so the watcher debounces those bursts
/// into one recompile.
/// </summary>
internal sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly ITimer _timer;

    /// <summary>
    /// Fires <paramref name="action"/> once triggers stay quiet for <paramref name="delay"/>.
    /// <paramref name="timeProvider"/> supplies the clock (defaults to the system clock); a
    /// test injects a fake one to drive the debounce deterministically, without real waiting.
    /// </summary>
    public Debouncer(TimeSpan delay, Action action, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delay = delay;
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(
            _ => action(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Registers activity; restarts the quiet-period countdown.</summary>
    public void Trigger() => _timer.Change(_delay, Timeout.InfiniteTimeSpan);

    /// <inheritdoc />
    public void Dispose() => _timer.Dispose();
}
