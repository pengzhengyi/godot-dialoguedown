using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.ConfigurationFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class ConfiguredSpeakerBuilderTests
{
    [Fact]
    public void ToDeclaration_CarriesNameAndId()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(ConfiguredSpeaker("Alice", "A"));

        Assert.Equal("Alice", declaration.Name);
        Assert.Equal("A", declaration.Id);
        Assert.Empty(declaration.Tags);
    }

    [Fact]
    public void ToDeclaration_ReservedTags_BecomeReservedTags()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(DefaultConfiguredSpeaker("Narrator"));

        var reserved = Assert.IsType<ReservedTag>(Assert.Single(declaration.Tags));
        Assert.Equal("default", reserved.Name);
        Assert.Null(reserved.Value);
    }

    [Fact]
    public void ToDeclaration_CustomTags_BecomeCustomTags()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(
            ConfiguredSpeaker("Alice", customTags: [ConfiguredTag("mood", "happy")]));

        var custom = Assert.IsType<CustomTag>(Assert.Single(declaration.Tags));
        Assert.Equal("mood", custom.Name);
        Assert.Equal("happy", custom.Value);
    }

    [Fact]
    public void ToDeclaration_UsesAnEmptySyntheticSpan()
    {
        var declaration = ConfiguredSpeakerBuilder.ToDeclaration(ConfiguredSpeaker("Alice"));

        Assert.True(declaration.Span.IsEmpty);
    }
}
