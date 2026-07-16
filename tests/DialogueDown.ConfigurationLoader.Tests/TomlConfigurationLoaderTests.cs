using DialogueDown.Configuration;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlConfigurationLoaderTests
{
    private const string SourceName = "dialogue.toml";

    [Fact]
    public void Parse_NullToml_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Parse(null!, SourceName));
    }

    [Fact]
    public void Parse_NullSourceName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Parse("", null!));
    }

    [Fact]
    public void Parse_NoSpeakers_ReturnsDefaultOptions()
    {
        CompilerOptions options = TomlConfigurationLoader.Parse(string.Empty, SourceName);

        Assert.Same(CompilerOptions.Default, options);
    }

    [Fact]
    public void Parse_WithSpeakers_WrapsThemInOptions()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            id = "A"
            """;

        CompilerOptions options = TomlConfigurationLoader.Parse(toml, SourceName);

        ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
    }

    [Fact]
    public void Load_NullPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TomlConfigurationLoader.Load(null!));
    }

    [Fact]
    public void Load_ReadsFileIntoOptions()
    {
        var toml = """
            [[speakers]]
            name = "Alice"
            """;
        string path = Path.Combine(Path.GetTempPath(), $"dialogue-{Guid.NewGuid():N}.toml");
        File.WriteAllText(path, toml);

        try
        {
            CompilerOptions options = TomlConfigurationLoader.Load(path);

            ConfiguredSpeaker speaker = Assert.Single(options.Speakers);
            Assert.Equal("Alice", speaker.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
