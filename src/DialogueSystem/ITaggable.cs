namespace DialogueSystem;

internal interface ITaggable
{
    internal IReadOnlySet<ITag> Tags { get; }
}
