using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// A build-once index of every node in a desugared tree, grouped by type, so a semantic
/// sub-pass can query <see cref="OfType{T}"/> instead of walking the tree itself. The tree
/// is traversed once (see <see cref="ScriptNodeExtensions.DescendantsAndSelf"/>); each node
/// is grouped under **every** type in its inheritance chain (its concrete type up to
/// <see cref="ScriptNode"/>) in document order, so a base-type query such as
/// <c>OfType&lt;Speaker&gt;()</c> works and results read top-to-bottom.
/// </summary>
internal sealed class DialogueTreeIndex
{
    private readonly ILookup<Type, ScriptNode> _byType;

    private DialogueTreeIndex(ILookup<Type, ScriptNode> byType) => _byType = byType;

    /// <summary>Walks <paramref name="document"/> once and indexes every node by type.</summary>
    public static DialogueTreeIndex Build(DesugaredScriptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // A lookup is a read-only multimap: it keeps document order within each type and
        // returns an empty sequence for a type that was never seen, so OfType needs no
        // missing-key handling.
        var byType = document.Body
            .SelectMany(block => block.DescendantsAndSelf())
            .SelectMany(
                node => node.TypeChainToScriptNode(),
                (node, type) => (Type: type, Node: node))
            .ToLookup(entry => entry.Type, entry => entry.Node);

        return new DialogueTreeIndex(byType);
    }

    /// <summary>Every indexed node of type <typeparamref name="T"/>, in document order.</summary>
    public IEnumerable<T> OfType<T>()
        where T : ScriptNode =>
        _byType[typeof(T)].Cast<T>();
}
