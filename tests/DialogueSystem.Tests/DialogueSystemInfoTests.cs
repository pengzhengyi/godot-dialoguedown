namespace DialogueSystem.Tests;

public sealed class DialogueSystemInfoTests
{
    [Fact]
    public void Name_IsDialogueSystem()
    {
        Assert.Equal("DialogueSystem", DialogueSystemInfo.Name);
    }
}
