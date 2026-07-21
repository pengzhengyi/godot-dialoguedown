namespace DialogueDown.Visualization.Live;

/// <summary>
/// Coalesces a burst of rapid triggers into a single action, fired once the triggers go quiet for
/// the debounce delay. Editors save by writing several times (or writing a temp file and
/// renaming), so the watcher debounces those bursts into one recompile.
/// </summary>
/// <remarks>
/// Runs are serialized: the action never runs concurrently with itself. A trigger (or a timer that
/// fires) while a run is in progress is coalesced into a single follow-up run scheduled after the
/// current one finishes, so a slow compile can never be overtaken by a newer one that then
/// broadcasts a stale result before it — the newest run always broadcasts last.
/// </remarks>
internal sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Action _action;
    private readonly ITimer _timer;
    private readonly object _gate = new();

    // Whether the action is running now, and whether a trigger arrived during that run and still
    // needs one follow-up run once it finishes.
    private bool _running;
    private bool _pending;

    /// <summary>
    /// Fires <paramref name="action"/> once triggers stay quiet for <paramref name="delay"/>.
    /// <paramref name="timeProvider"/> supplies the clock (defaults to the system clock); a
    /// test injects a fake one to drive the debounce deterministically, without real waiting.
    /// </summary>
    public Debouncer(TimeSpan delay, Action action, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delay = delay;
        _action = action;
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(
            _ => Fire(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Registers activity; restarts the quiet-period countdown, or coalesces into an active run.</summary>
    public void Trigger()
    {
        lock (_gate)
        {
            if (_running)
            {
                // A change during a run: remember it and let the current run finish, then run once
                // more — never a second concurrent run.
                _pending = true;
                return;
            }
        }

        _timer.Change(_delay, Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void Dispose() => _timer.Dispose();

    private void Fire()
    {
        lock (_gate)
        {
            if (_running)
            {
                // The timer fired while a run is still in progress (a race with Trigger): coalesce
                // it into one follow-up rather than running concurrently.
                _pending = true;
                return;
            }

            _running = true;
        }

        bool followUp = false;
        try
        {
            _action();
        }
        finally
        {
            // Always clear _running — even if the action threw — so a failing refresh can never
            // wedge the debouncer and stop every later on-disk change from reloading.
            lock (_gate)
            {
                _running = false;
                followUp = _pending;
                _pending = false;
            }
        }

        if (followUp)
        {
            // Re-arm the debounce for the change that arrived during the run.
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }
}
