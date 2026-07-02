namespace DialogueSystem;

internal interface IEdge : Identifiable, ITaggable
{
    internal INode Source { get; }

    internal INode Target { get; }

    internal bool IsConnected { get; }
}
