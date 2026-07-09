namespace DialogueDown.Visualization.Live.Tests;

public sealed class DebouncerTests
{
    [Fact]
    public async Task Trigger_MultipleTimesWithinTheDelay_FiresOnce()
    {
        var count = 0;
        using var debouncer = new Debouncer(
            TimeSpan.FromMilliseconds(150),
            () => Interlocked.Increment(ref count));

        debouncer.Trigger();
        debouncer.Trigger();
        debouncer.Trigger();
        await Task.Delay(450);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Trigger_AfterFiring_FiresAgain()
    {
        var count = 0;
        using var debouncer = new Debouncer(
            TimeSpan.FromMilliseconds(100),
            () => Interlocked.Increment(ref count));

        debouncer.Trigger();
        await Task.Delay(300);
        debouncer.Trigger();
        await Task.Delay(300);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Trigger_ThenQuiet_DoesNotFireEarly()
    {
        var count = 0;
        using var debouncer = new Debouncer(
            TimeSpan.FromMilliseconds(300),
            () => Interlocked.Increment(ref count));

        debouncer.Trigger();
        await Task.Delay(100); // still within the debounce window

        Assert.Equal(0, count);
    }
}
