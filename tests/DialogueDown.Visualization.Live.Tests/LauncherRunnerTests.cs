using System.Diagnostics;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LauncherRunnerTests
{
    [Fact]
    public async Task RunAsync_InvalidRoot_ReturnsOne()
    {
        var error = new StringWriter();
        var runner = new LauncherRunner(new FakeBrowserLauncher());

        var code = await runner.RunAsync(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
            source: null,
            LaunchMode.View,
            port: null,
            noOpen: true,
            new StringWriter(),
            error,
            CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("not a directory", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_OpensTheLauncherUrl_AndStopsOnCancellation()
    {
        using var tree = new TempTree();
        tree.File("scene.dialogue.md", "# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new LauncherRunner(browser);
        using var stop = new CancellationTokenSource();

        var task = runner.RunAsync(
            tree.Root, source: null, LaunchMode.View, port: 0, noOpen: false,
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        var url = Assert.Single(browser.Opened);
        Assert.StartsWith("http://127.0.0.1:", url);

        using var client = new HttpClient { BaseAddress = new Uri(url) };
        Assert.Contains("Open a script", await client.GetStringAsync("/"));

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met in time.");
            }

            await Task.Delay(25);
        }
    }
}
