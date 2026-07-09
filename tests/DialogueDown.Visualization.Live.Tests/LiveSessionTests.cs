using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveSessionTests
{
    [Fact]
    public void RenderInitialHtml_MarksThePayloadLive()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var html = session.RenderInitialHtml();

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"live\":true", html);
    }

    [Fact]
    public void CurrentDocumentJson_CarriesPathSourceAndStages()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var json = session.CurrentDocumentJson();

        Assert.Contains($"\"path\":", json);
        Assert.Contains("\"source\":\"# Scene\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Refresh_BroadcastsAReloadWithTheCurrentContent()
    {
        using var doc = new TempDocument("# First");
        var session = new LiveSession(doc.Path);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(doc.Path, "# Second");

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# Second", received.Data);
    }

    [Fact]
    public void Refresh_MissingDocument_BroadcastsAProblem()
    {
        var session = new LiveSession("/tmp/missing-live.dialogue.md");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
    }
}
