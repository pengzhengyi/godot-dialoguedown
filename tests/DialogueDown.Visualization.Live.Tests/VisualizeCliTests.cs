using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class VisualizeCliTests
{
    [Fact]
    public void Parse_ValidArguments_HasNoErrors()
    {
        var cli = VisualizeCli.Create(new FakeBrowserLauncher());

        var result = cli.Parse(["scene.dialogue.md", "--output", "out.html", "--no-open"]);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WatchWithRenderRoot_HasNoErrors()
    {
        var cli = VisualizeCli.Create(new FakeBrowserLauncher());

        var result = cli.Parse(["scene.dialogue.md", "--watch", "--render-root", "/tmp/gallery"]);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Invoke_ValidDocument_RendersReportAndReturnsZero()
    {
        using var doc = new TempDocument();
        var browser = new FakeBrowserLauncher();
        var output = Path.Combine(Path.GetTempPath(), $"dd-cli-{Guid.NewGuid():N}.html");
        var cli = VisualizeCli.Create(browser);

        try
        {
            var code = cli.Parse([doc.Path, "-o", output, "--no-open"]).Invoke();

            Assert.Equal(0, code);
            Assert.True(File.Exists(output));
            Assert.Empty(browser.Opened); // --no-open
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public void Invoke_MissingFileArgument_ReturnsNonZero()
    {
        var cli = VisualizeCli.Create(new FakeBrowserLauncher());

        var code = cli.Parse([]).Invoke();

        Assert.NotEqual(0, code);
    }

    [Fact]
    public async Task Invoke_Watch_StartsAServerAndStopsWhenCancelled()
    {
        using var doc = new TempDocument();
        var browser = new FakeBrowserLauncher();
        var cli = VisualizeCli.Create(browser);
        using var stop = new CancellationTokenSource();

        var task = cli.Parse([doc.Path, "--watch"]).InvokeAsync(cancellationToken: stop.Token);
        // The watch branch opens the browser once the server is up.
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
