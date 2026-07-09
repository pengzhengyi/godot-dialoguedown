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
}
