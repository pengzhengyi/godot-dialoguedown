using DialogueDown.Markdown;

namespace DialogueDown.Tests.Markdown;

public sealed class SourceSpanTests
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, 5, 5)]
    [InlineData(3, 4, 7)]
    public void Constructor_ValidStartAndLength_ExposesStartLengthAndEnd(
        int start, int length, int expectedEnd)
    {
        var span = new SourceSpan(start, length);

        Assert.Equal(start, span.Start);
        Assert.Equal(length, span.Length);
        Assert.Equal(expectedEnd, span.End);
    }

    [Fact]
    public void Constructor_NegativeStart_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(-1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveLength_Throws(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(0, length));
    }

    [Fact]
    public void Equality_SameStartAndLength_AreEqual()
    {
        Assert.Equal(new SourceSpan(2, 3), new SourceSpan(2, 3));
    }

    [Fact]
    public void Equality_DifferentStartOrLength_AreNotEqual()
    {
        Assert.NotEqual(new SourceSpan(2, 3), new SourceSpan(2, 4));
        Assert.NotEqual(new SourceSpan(2, 3), new SourceSpan(1, 3));
    }
}
