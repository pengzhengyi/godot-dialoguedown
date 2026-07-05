namespace DialogueDown;

internal interface ITaggable
{
    internal IReadOnlySet<ITag> Tags { get; }
}
