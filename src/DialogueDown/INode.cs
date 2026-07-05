namespace DialogueDown;

internal interface INode : Identifiable, ITaggable
{
    internal IReadOnlySet<IEdge> Edges { get; }
}
