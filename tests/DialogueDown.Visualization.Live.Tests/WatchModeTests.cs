using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class WatchModeTests
{
    [Fact]
    public async Task RunAsync_BadDocument_ReturnsOne()
    {
        var error = new StringWriter();

        var code = await WatchMode.RunAsync(
            "/tmp/missing.dialogue.md",
            port: null,
            noOpen: true,
            renderRoot: null,
            new FakeBrowserLauncher(),
            new FakeHostConsent(allow: false),
            new StringWriter(),
            error,
            CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_OpensBrowserWithLoopbackUrl_AndStopsOnCancellation()
    {
        using var doc = new TempDocument();
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = WatchMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        var url = Assert.Single(browser.Opened);
        Assert.StartsWith("http://127.0.0.1:", url);

        stop.Cancel();
        Assert.Equal(0, await task);
    }

    [Fact]
    public async Task RunAsync_HotReloadsWhenTheDocumentChanges()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var token = timeout.Token;
        using var doc = new TempDocument("# First");
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = WatchMode.RunAsync(
            doc.Path, port: 0, noOpen: false, renderRoot: null, browser, new FakeHostConsent(allow: false),
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        using var response = await client.GetAsync(
            "/api/events", HttpCompletionOption.ResponseHeadersRead, token);
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);

        await Task.Delay(300, token); // let the SSE subscription settle
        File.WriteAllText(doc.Path, "# Changed");

        var sawChange = false;
        string? line;
        while ((line = await reader.ReadLineAsync(token)) is not null)
        {
            if (line.Contains("# Changed", StringComparison.Ordinal))
            {
                sawChange = true;
                break;
            }
        }

        Assert.True(sawChange);

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_ImageOutsideFolder_WithRenderRoot_ServesReportAtSubPathWithoutPrompting()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("proj/scene.dialogue.md", "# Scene\n\n![p](../shared/pic.png)");
        var pic = tree.File("shared/pic.png");
        await File.WriteAllBytesAsync(pic, new byte[] { 4, 2 });
        var browser = new FakeBrowserLauncher();
        var consent = new FakeHostConsent(allow: false);
        using var stop = new CancellationTokenSource();

        var task = WatchMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: tree.Root, browser, consent,
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.False(consent.WasAsked); // explicit render root skips the prompt
        Assert.EndsWith("/proj/", browser.Opened[0]);
        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        var response = await client.GetAsync("/shared/pic.png");
        Assert.True(response.IsSuccessStatusCode);

        stop.Cancel();
        await task;
    }

    [Fact]
    public async Task RunAsync_RenderRootNotContainingDocument_ReturnsOneWithoutServing()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("proj/scene.dialogue.md", "# Scene");
        var elsewhere = tree.Dir("elsewhere");
        var browser = new FakeBrowserLauncher();
        var error = new StringWriter();

        var code = await WatchMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: elsewhere, browser,
            new FakeHostConsent(allow: false), new StringWriter(), error, CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("render root", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(browser.Opened);
    }

    [Fact]
    public async Task RunAsync_ImageOutsideFolder_Declined_ServesDocumentFolderOnly()
    {
        using var tree = new TempTree();
        var documentPath = tree.File("proj/scene.dialogue.md", "# Scene\n\n![p](../shared/pic.png)");
        tree.File("shared/pic.png");
        var consent = new FakeHostConsent(allow: false);
        var browser = new FakeBrowserLauncher();
        using var stop = new CancellationTokenSource();

        var task = WatchMode.RunAsync(
            documentPath, port: 0, noOpen: false, renderRoot: null, browser, consent,
            new StringWriter(), new StringWriter(), stop.Token);
        await WaitUntilAsync(() => browser.Opened.Count > 0, TimeSpan.FromSeconds(10));

        Assert.True(consent.WasAsked);
        Assert.EndsWith("/", browser.Opened[0]);
        Assert.DoesNotContain("/proj/", browser.Opened[0]);
        using var client = new HttpClient { BaseAddress = new Uri(browser.Opened[0]) };
        var response = await client.GetAsync("/shared/pic.png");
        Assert.False(response.IsSuccessStatusCode);

        stop.Cancel();
        await task;
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
