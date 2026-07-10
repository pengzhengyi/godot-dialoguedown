using System.Net;
using System.Net.Http.Json;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LauncherServerTests
{
    private const string LauncherHtml = "<!doctype html><title>Launcher</title>";

    [Fact]
    public async Task Root_ServesTheLauncherPage()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        Assert.Equal(LauncherHtml, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task Browse_ListsSubdirectoriesAndDialogueSourcesAsAbsolutePaths()
    {
        using var tree = new TempTree();
        tree.File("root/a.dialogue.md", "# A");
        tree.File("root/notes.md");
        tree.Dir("root/proj");
        await using var server = await Started(tree);
        using var client = Client(server);

        var json = await client.GetStringAsync("/api/browse?path=");

        Assert.Contains(Path.Combine(tree.Dir("root"), "proj").Replace("\\", "\\\\"), json);
        Assert.Contains("a.dialogue.md", json);
        Assert.DoesNotContain("notes.md", json);
    }

    [Fact]
    public async Task Browse_IsUnconfined_CanListOutsideTheStartRoot()
    {
        using var tree = new TempTree();
        tree.Dir("root");
        tree.File("outside/x.dialogue.md");
        await using var server = await Started(tree);
        using var client = Client(server);

        var response = await client.GetAsync($"/api/browse?path={Uri.EscapeDataString(tree.Dir("outside"))}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("x.dialogue.md", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Open_ValidSource_RedirectsToReportAndServesIt()
    {
        using var tree = new TempTree();
        var source = tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await Open(client, tree.Dir("root"), source, "static");

        Assert.Equal(HttpStatusCode.SeeOther, open.StatusCode);
        Assert.Equal("/r/proj/", open.Headers.Location!.ToString());

        var html = await client.GetStringAsync("/r/proj/");
        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"static\"", html);
    }

    [Fact]
    public async Task Open_WatchMode_ServesReportInWatchMode()
    {
        using var tree = new TempTree();
        var source = tree.File("root/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await Open(client, tree.Dir("root"), source, "watch");

        Assert.Equal("/r/", open.Headers.Location!.ToString());
        Assert.Contains("\"mode\":\"watch\"", await client.GetStringAsync("/r/"));
    }

    [Fact]
    public async Task Open_ChoosingAHigherRoot_ChangesTheReportPath()
    {
        using var tree = new TempTree();
        var source = tree.File("root/proj/scene.dialogue.md", "# Scene");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var deep = await Open(client, tree.Dir("root/proj"), source, "static");
        var shallow = await Open(client, tree.Dir("root"), source, "static");

        Assert.Equal("/r/", deep.Headers.Location!.ToString());
        Assert.Equal("/r/proj/", shallow.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Open_SourceOutsideTheChosenRoot_NotFound()
    {
        using var tree = new TempTree();
        tree.Dir("root");
        var outside = tree.File("outside/x.dialogue.md", "# X");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        var open = await Open(client, tree.Dir("root"), outside, "static");

        Assert.Equal(HttpStatusCode.NotFound, open.StatusCode);
    }

    [Fact]
    public async Task Open_NonDialogueSource_NotFound()
    {
        using var tree = new TempTree();
        var notes = tree.File("root/notes.md");
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        Assert.Equal(HttpStatusCode.NotFound, (await Open(client, tree.Dir("root"), notes, "static")).StatusCode);
    }

    [Fact]
    public async Task Report_BeforeOpen_NotFound()
    {
        using var tree = new TempTree();
        await using var server = await Started(tree);
        using var client = Client(server);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/r/")).StatusCode);
    }

    [Fact]
    public async Task OpenedSource_ServesRelativeAssetsUnderReportMount()
    {
        using var tree = new TempTree();
        var source = tree.File("root/proj/scene.dialogue.md", "# Scene\n\n![pic](art/pic.png)");
        await File.WriteAllBytesAsync(tree.File("root/proj/art/pic.png"), [1, 2, 3, 4]);
        await using var server = await Started(tree);
        using var client = Client(server, followRedirects: false);

        await Open(client, tree.Dir("root"), source, "static");
        var asset = await client.GetAsync("/r/proj/art/pic.png");

        Assert.True(asset.IsSuccessStatusCode);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await asset.Content.ReadAsByteArrayAsync());
    }

    private static Task<HttpResponseMessage> Open(HttpClient client, string root, string source, string mode) =>
        client.PostAsJsonAsync("/api/open", new { root, source, mode });

    private static async Task<LauncherServer> Started(TempTree tree)
    {
        var server = new LauncherServer(tree.Dir("root"), LauncherHtml);
        await server.StartAsync();
        return server;
    }

    private static HttpClient Client(LauncherServer server, bool followRedirects = true)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = followRedirects };
        return new HttpClient(handler) { BaseAddress = new Uri(server.BaseUrl) };
    }
}
