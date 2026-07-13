using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class StringExtensionsTests
{
    [Fact]
    public void NullIfEmpty_NonEmpty_IsUnchanged() => Assert.Equal("hi", "hi".NullIfEmpty());

    [Fact]
    public void NullIfEmpty_Empty_IsNull() => Assert.Null(string.Empty.NullIfEmpty());

    [Fact]
    public void NullIfEmpty_Null_IsNull() => Assert.Null(((string?)null).NullIfEmpty());
}
