namespace DialogueDown;

internal interface IDialogue
{
    internal ISpeaker Speaker { get; }

    internal ISpeech Speech { get; }
}
