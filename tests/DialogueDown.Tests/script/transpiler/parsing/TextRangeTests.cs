using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class TextRangeTests
{
    [Fact]
    public void ExposesStartLengthAndComputedEnd()
    {
        var range = new TextRange(5, 3);

        Assert.Equal(5, range.Start);
        Assert.Equal(3, range.Length);
        Assert.Equal(8, range.End);
    }

    [Fact]
    public void AllowsEmptyRange()
    {
        var range = new TextRange(5, 0);

        Assert.Equal(0, range.Length);
        Assert.Equal(5, range.End);
    }

    [Fact]
    public void RejectsNegativeStart() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(-1, 0));

    [Fact]
    public void RejectsNegativeLength() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(0, -1));
}
