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
}
