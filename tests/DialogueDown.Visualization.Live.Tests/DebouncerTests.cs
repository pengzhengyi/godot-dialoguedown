using Microsoft.Extensions.Time.Testing;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class DebouncerTests
{
    [Fact]
    public void Trigger_MultipleTimesWithinTheDelay_FiresOnce()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(150), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(150)); // quiet for the full delay after the last trigger

        Assert.Equal(1, count);
    }

    [Fact]
    public void Trigger_AfterFiring_FiresAgain()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(100), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // fires once
        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // fires again

        Assert.Equal(2, count);
    }

    [Fact]
    public void Fire_WhenTheActionThrows_ClearsRunningSoLaterTriggersStillFire()
    {
        // A refresh that throws (e.g. an unreadable file) must not wedge the debouncer: clearing
        // the running flag in a finally keeps every later on-disk change producing a reload. A
        // manual timer drives the callback so the test does not depend on a fake timer re-firing
        // after a throw.
        var provider = new ManualTimeProvider();
        var runs = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            runs++;
            if (runs == 1)
            {
                throw new InvalidOperationException("boom");
            }
        }, provider);
        var timer = provider.Timer!;

        Assert.Throws<InvalidOperationException>(timer.Fire); // the throwing run propagates
        Assert.Equal(1, runs);

        // Not wedged: a later change re-arms the timer (a no-op if _running were stuck true)...
        var armed = timer.ChangeCount;
        debouncer.Trigger();
        Assert.True(timer.ChangeCount > armed);

        // ...and that re-armed fire actually runs the action again.
        timer.Fire();
        Assert.Equal(2, runs);
    }

    [Fact]
    public void Trigger_ThenQuiet_DoesNotFireEarly()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(300), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // still within the debounce window

        Assert.Equal(0, count);
    }

    [Fact]
    public void Trigger_DuringTheAction_CoalescesIntoExactlyOneSerializedFollowUpRun()
    {
        // A change that lands while the debounced action is still running (a slow compile) must
        // not run concurrently with it; it is coalesced into exactly one follow-up run after the
        // current one finishes, so an older run can never broadcast after a newer one.
        var time = new FakeTimeProvider();
        var runs = 0;
        var maxConcurrent = 0;
        var concurrent = 0;
        Debouncer? debouncer = null;
        debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            var now = Interlocked.Increment(ref concurrent);
            maxConcurrent = Math.Max(maxConcurrent, now);
            runs++;
            if (runs == 1)
            {
                debouncer!.Trigger(); // a file change arrives mid-run
            }

            Interlocked.Decrement(ref concurrent);
        }, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(10)); // fires run #1, which re-triggers once

        Assert.Equal(1, runs); // the coalesced change is deferred, not run inline or concurrently
        Assert.Equal(1, maxConcurrent);

        time.Advance(TimeSpan.FromMilliseconds(10)); // the coalesced follow-up fires
        Assert.Equal(2, runs);

        time.Advance(TimeSpan.FromMilliseconds(100)); // nothing more is pending
        Assert.Equal(2, runs);

        debouncer.Dispose();
    }

    [Fact]
    public void Trigger_ManyTimesDuringOneRun_CoalescesToASingleFollowUp()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        Debouncer? debouncer = null;
        debouncer = new Debouncer(TimeSpan.FromMilliseconds(10), () =>
        {
            runs++;
            if (runs == 1)
            {
                debouncer!.Trigger();
                debouncer!.Trigger();
                debouncer!.Trigger();
            }
        }, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(10));
        time.Advance(TimeSpan.FromMilliseconds(10));
        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(2, runs); // three mid-run triggers collapse into one follow-up run

        debouncer.Dispose();
    }

    // A time provider whose single timer fires only when the test calls Fire(), and that records
    // how many times it was (re-)armed — enough to drive the debounce deterministically, including
    // a callback that throws, without depending on a fake clock's post-throw behavior.
    private sealed class ManualTimeProvider : TimeProvider
    {
        public ManualTimer? Timer { get; private set; }

        public override ITimer CreateTimer(
            TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            Timer = new ManualTimer(callback, state);
    }

    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        public int ChangeCount { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ChangeCount++;
            return true;
        }

        public void Fire() => callback(state);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
