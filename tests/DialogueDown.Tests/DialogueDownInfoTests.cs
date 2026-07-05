namespace DialogueDown.Tests;

public sealed class DialogueDownInfoTests
{
    [Fact]
    public void Name_IsDialogueDown()
    {
        Assert.Equal("DialogueDown", DialogueDownInfo.Name);
    }
}
