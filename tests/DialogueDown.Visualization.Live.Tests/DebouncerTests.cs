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
    public void Trigger_ThenQuiet_DoesNotFireEarly()
    {
        var time = new FakeTimeProvider();
        var count = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(300), () => count++, time);

        debouncer.Trigger();
        time.Advance(TimeSpan.FromMilliseconds(100)); // still within the debounce window

        Assert.Equal(0, count);
    }
}
