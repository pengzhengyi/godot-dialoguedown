namespace DialogueDown.Graph;

internal interface ITaggable
{
    internal IReadOnlySet<ITag> Tags { get; }
}
