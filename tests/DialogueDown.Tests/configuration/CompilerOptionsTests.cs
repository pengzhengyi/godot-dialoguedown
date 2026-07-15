using DialogueDown.Configuration;

namespace DialogueDown.Tests.Configuration;

public sealed class CompilerOptionsTests
{
    [Fact]
    public void Default_HasNoConfiguredSpeakers() =>
        Assert.Empty(CompilerOptions.Default.Speakers);

    [Fact]
    public void Speakers_AreEmptyOnAFreshInstance() =>
        Assert.Empty(new CompilerOptions().Speakers);

    [Fact]
    public void ForSemanticAnalyzer_ExposesTheConfiguredSpeakers()
    {
        var narrator = new ConfiguredSpeaker(
            "Narrator", "narrator",
            CustomTags: [new ConfiguredTag("mood", "happy")],
            ReservedTags: [new ConfiguredTag("default")]);
        var options = new CompilerOptions { Speakers = [narrator] };

        var configured = Assert.Single(options.ForSemanticAnalyzer().ConfiguredSpeakers);
        Assert.Equal("Narrator", configured.Name);
        Assert.Equal("narrator", configured.Id);
        Assert.Equal([new ConfiguredTag("mood", "happy")], configured.CustomTags);
        Assert.Equal([new ConfiguredTag("default")], configured.ReservedTags);
    }
}
