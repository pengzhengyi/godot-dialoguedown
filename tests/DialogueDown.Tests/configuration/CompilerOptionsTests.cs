using DialogueDown.Configuration;

namespace DialogueDown.Tests.Configuration;

public sealed class CompilerOptionsTests
{
    [Fact]
    public void Default_HasNoConfiguredDefaultSpeaker() =>
        Assert.Null(CompilerOptions.Default.DefaultSpeakerName);

    [Fact]
    public void DefaultSpeakerName_IsUnsetOnAFreshInstance() =>
        Assert.Null(new CompilerOptions().DefaultSpeakerName);

    [Fact]
    public void Semantics_CarriesTheConfiguredDefaultSpeakerName()
    {
        var options = new CompilerOptions { DefaultSpeakerName = "Narrator" };

        Assert.Equal("Narrator", options.Semantics.DefaultSpeakerName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Semantics_TreatsABlankNameAsUnset(string? blank)
    {
        var options = new CompilerOptions { DefaultSpeakerName = blank };

        Assert.Null(options.Semantics.DefaultSpeakerName);
    }
}
