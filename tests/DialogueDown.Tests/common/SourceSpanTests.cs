using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

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

    [Theory]
    [InlineData(0, 2, 2, 3, 0, 5)] // contiguous: [0,2) through [2,5)
    [InlineData(0, 2, 6, 1, 0, 7)] // gapped: [0,2) through [6,7), spanning the gap
    [InlineData(3, 4, 3, 4, 3, 7)] // single span: covers itself
    public void Covering_EndFollowsStart_SpansFromStartBeginningToEndEnding(
        int startPos, int startLen, int endPos, int endLen, int expectedStart, int expectedEnd)
    {
        var covering = SourceSpan.Covering(new SourceSpan(startPos, startLen), new SourceSpan(endPos, endLen));

        Assert.Equal(expectedStart, covering.Start);
        Assert.Equal(expectedEnd, covering.End);
    }

    [Theory]
    [InlineData(5, 2, 1, 2)] // end begins before start
    [InlineData(2, 5, 3, 1)] // end ends before start ends
    public void Covering_EndPrecedesStart_Throws(
        int startPos, int startLen, int endPos, int endLen) =>
        Assert.Throws<ArgumentException>(
            () => SourceSpan.Covering(new SourceSpan(startPos, startLen), new SourceSpan(endPos, endLen)));
}
