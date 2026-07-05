namespace DialogueDown.Tests;

public sealed class TagTests
{
    [Fact]
    public void Constructor_StoresIdentityAndInternalId()
    {
        var tag = new Tag("main", "Main", 7);

        Assert.Equal("main", tag.Id);
        Assert.Equal("Main", tag.Name);
        Assert.Equal(7, tag.InternalId);
    }
}
