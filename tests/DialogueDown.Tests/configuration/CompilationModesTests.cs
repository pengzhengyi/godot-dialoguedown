using DialogueDown.Configuration;

namespace DialogueDown.Tests.Configuration;

public sealed class CompilationModesTests
{
    [Theory]
    [InlineData("stage-boundary", CompilationMode.StageBoundary)]
    [InlineData("best-effort", CompilationMode.BestEffort)]
    public void TryParse_ASettableName_ReturnsItsMode(string name, CompilationMode expected) =>
        Assert.Equal(expected, CompilationModes.TryParse(name));

    [Theory]
    [InlineData("fail-fast")] // an embedding contract, not a settable reporting mode
    [InlineData("turbo")]
    [InlineData("StageBoundary")] // the enum name is not the setting name
    [InlineData("")]
    public void TryParse_ANonSettableName_ReturnsNull(string name) =>
        Assert.Null(CompilationModes.TryParse(name));

    [Fact]
    public void TryParse_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CompilationModes.TryParse(null!));

    [Fact]
    public void SettableNamesDescription_ListsTheSettableModes() =>
        Assert.Equal("'stage-boundary' or 'best-effort'", CompilationModes.SettableNamesDescription);
}
