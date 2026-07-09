using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParseInputTests
{
    [Fact]
    public void Range_SpansTheTextFromItsAnchorPosition()
    {
        var range = new ParseInput("abc", 5).Range;

        Assert.Equal(5, range.Start);
        Assert.Equal(3, range.Length);
    }

    [Fact]
    public void Advance_DropsConsumedText_AndMovesThePosition()
    {
        var rest = new ParseInput("abcdef", 5).Advance(3);

        Assert.Equal("def", rest.Text);
        Assert.Equal(8, rest.Position);
    }

    [Fact]
    public void Advance_ByZero_ReturnsTheSameInput()
    {
        var input = new ParseInput("abc", 5);

        Assert.Equal(input, input.Advance(0));
    }

    [Fact]
    public void Advance_ToEndOfText_LeavesAnEmptyRemainder()
    {
        var rest = new ParseInput("abc", 5).Advance(3);

        Assert.Equal("", rest.Text);
        Assert.Equal(8, rest.Position);
    }

    [Fact]
    public void Advance_NegativeAmount_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseInput("abc", 5).Advance(-1));

    [Fact]
    public void Advance_PastEndOfText_Throws() =>
        // A parser cannot consume more than the input holds; advancing past the end
        // signals a miscount, so it throws rather than clamping.
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseInput("abc", 5).Advance(4));
}
