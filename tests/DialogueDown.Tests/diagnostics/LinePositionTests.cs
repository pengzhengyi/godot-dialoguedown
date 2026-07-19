using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class LinePositionTests
{
    [Fact]
    public void FormatsAsLineCommaColumn() =>
        Assert.Equal("3,7", new LinePosition(3, 7).ToString());

    [Fact]
    public void CarriesItsLineAndColumn()
    {
        var position = new LinePosition(2, 5);

        Assert.Equal(2, position.Line);
        Assert.Equal(5, position.Column);
    }

    [Fact]
    public void EqualsByValue() =>
        Assert.Equal(new LinePosition(4, 9), new LinePosition(4, 9));
}
