namespace DialogueDown.Graph;

internal interface INode : Identifiable, ITaggable
{
    internal IReadOnlySet<IEdge> Edges { get; }
}
