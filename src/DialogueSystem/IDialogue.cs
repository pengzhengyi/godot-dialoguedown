namespace DialogueSystem;

internal interface IDialogue
{
    internal ISpeaker Speaker { get; }

    internal ISpeech Speech { get; }
}
