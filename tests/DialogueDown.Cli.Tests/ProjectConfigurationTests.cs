using DialogueDown.Cli.Tests.Support;
using DialogueDown.Configuration;

namespace DialogueDown.Cli.Tests;

public sealed class ProjectConfigurationTests
{
    private const string NarratorConfig = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;

    [Fact]
    public void Resolve_NoConfigAndNoFile_ReturnsDefault()
    {
        using var dir = new TempDir();

        var options = new ProjectConfiguration().Resolve(null, dir.Path);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Resolve_ExplicitConfig_LoadsThatFile()
    {
        using var dir = new TempDir();
        var configPath = dir.Write("elsewhere/custom.toml", NarratorConfig);

        var options = new ProjectConfiguration().Resolve(configPath, dir.Path);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_DiscoversFileInStartDirectory()
    {
        using var dir = new TempDir();
        dir.Write(ProjectConfiguration.FileName, NarratorConfig);

        var options = new ProjectConfiguration().Resolve(null, dir.Path);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_WalksUpToNearestFile()
    {
        using var dir = new TempDir();
        dir.Write(ProjectConfiguration.FileName, NarratorConfig);
        var nested = dir.Dir("act1/scene3");

        var options = new ProjectConfiguration().Resolve(null, nested);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_NearestFileWins()
    {
        using var dir = new TempDir();
        dir.Write(ProjectConfiguration.FileName, NarratorConfig);
        var nested = dir.Dir("act1");
        dir.Write($"act1/{ProjectConfiguration.FileName}", """
            [[speakers]]
            name = "Alice"
            """);

        var options = new ProjectConfiguration().Resolve(null, nested);

        Assert.Equal("Alice", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void Resolve_DoesNotReadConfigAboveTheBoundary()
    {
        using var dir = new TempDir();
        dir.Write(ProjectConfiguration.FileName, NarratorConfig);
        var boundary = dir.Dir("project");
        var nested = dir.Dir("project/act1");

        var options = new ProjectConfiguration().Resolve(null, nested, boundary);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Resolve_ReadsConfigAtTheBoundary()
    {
        using var dir = new TempDir();
        var boundary = dir.Dir("project");
        dir.Write($"project/{ProjectConfiguration.FileName}", NarratorConfig);
        var nested = dir.Dir("project/act1");

        var options = new ProjectConfiguration().Resolve(null, nested, boundary);

        Assert.Equal("Narrator", Assert.Single(options.Speakers).Name);
    }

    [Fact]
    public void ResolveApplied_NoFile_UsesDefaultsWithNoFile()
    {
        using var dir = new TempDir();

        var applied = new ProjectConfiguration().ResolveApplied(null, dir.Path);

        Assert.True(applied.UsesDefaultConfiguration);
        Assert.Null(applied.File);
        Assert.Same(CompilerOptions.Default, applied.Options);
    }

    [Fact]
    public void ResolveApplied_DiscoveredFile_CarriesItsPathTextAndOptions()
    {
        using var dir = new TempDir();
        var path = dir.Write(ProjectConfiguration.FileName, NarratorConfig);

        var applied = new ProjectConfiguration().ResolveApplied(null, dir.Path);

        Assert.True(applied.IsConfiguredFromFile);
        Assert.Equal(path, applied.File!.Path);
        Assert.Equal(NarratorConfig, applied.File.Source);
        Assert.Equal("Narrator", Assert.Single(applied.Options.Speakers).Name);
    }

    [Fact]
    public void ResolveApplied_ExplicitConfig_CarriesThatFile()
    {
        using var dir = new TempDir();
        var path = dir.Write("elsewhere/custom.toml", NarratorConfig);

        var applied = new ProjectConfiguration().ResolveApplied(path, dir.Path);

        Assert.True(applied.IsConfiguredFromFile);
        Assert.Equal(path, applied.File!.Path);
        Assert.Equal(NarratorConfig, applied.File.Source);
    }
}
