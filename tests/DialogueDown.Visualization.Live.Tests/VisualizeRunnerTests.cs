using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class VisualizeRunnerTests
{
    [Fact]
    public void RunStatic_WritesReportAndOpensIt()
    {
        using var doc = new TempDocument("# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        var code = runner.RunStatic(doc.Path, output: null, noOpen: false);

        Assert.Equal(0, code);
        var opened = Assert.Single(browser.Opened);
        Assert.EndsWith(".html", opened);
        Assert.True(File.Exists(opened));
        File.Delete(opened);
    }

    [Fact]
    public void RunStatic_Output_WritesToThePathWithoutOpening()
    {
        using var doc = new TempDocument("# Scene");
        var target = Path.Combine(Path.GetTempPath(), $"dd-vr-{Guid.NewGuid():N}.html");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        try
        {
            var code = runner.RunStatic(doc.Path, target, noOpen: true);

            Assert.Equal(0, code);
            Assert.True(File.Exists(target));
            Assert.Empty(browser.Opened);
        }
        finally
        {
            File.Delete(target);
        }
    }

    [Fact]
    public async Task RunWatchAsync_ServesAndOpensAUrl_ThenStopsOnCancel()
    {
        using var doc = new TempDocument("# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);
        using var stop = new CancellationTokenSource();

        var task = runner.RunWatchAsync(doc.Path, port: 0, noOpen: false, renderRoot: null, stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.StartsWith("http://127.0.0.1:", browser.Opened[0]);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        if (!condition())
        {
            throw new TimeoutException("Condition was not met in time.");
        }
    }
}
