using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Read-only traversal helpers over the dialogue AST, used by the semantic sub-passes.
/// The AST records stay plain data; walking their shape lives here so every pass shares
/// one description of the tree.
/// </summary>
internal static class ScriptNodeExtensions
{
    /// <summary>
    /// Yields <paramref name="node"/> and then each descendant, depth-first in document
    /// order (a node before its children). Returning a sequence lets callers compose with
    /// LINQ; the script's nesting is shallow, so recursion is safe.
    /// </summary>
    internal static IEnumerable<ScriptNode> DescendantsAndSelf(this ScriptNode node)
    {
        yield return node;
        foreach (var child in node.Children())
        {
            foreach (var descendant in child.DescendantsAndSelf())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// The visible text of a fragment sequence: every <see cref="Text"/> anywhere within the
    /// fragments, concatenated in document order. A styled run or a link label still
    /// contributes its words, so this reads a heading title or label as plain text.
    /// </summary>
    internal static string PlainText(this IEnumerable<InlineFragment> fragments) =>
        string.Concat(fragments
            .SelectMany(fragment => fragment.DescendantsAndSelf())
            .OfType<Text>()
            .Select(text => text.Content));

    /// <summary>
    /// The node's direct children in document order. Every node type is handled
    /// explicitly: containers return their child slots and leaves return nothing, so an
    /// unhandled type throws rather than being silently skipped when the AST grows.
    /// </summary>
    internal static IEnumerable<ScriptNode> Children(this ScriptNode node) => node switch
    {
        // Containers — their child slots, in document order.
        Line line => LineChildren(line),
        Choices choices => choices.Options,
        Choice choice => choice.Body,
        SceneHeading heading => heading.Title,
        SpeakerDeclaration declaration => declaration.Tags,
        PartialSpeakerDeclaration partial => partial.Tags,
        StyledText styled => styled.Children,
        Image image => image.Alt,
        Link link => link.Label,
        Jump jump => jump.Label,

        // Leaves — no children.
        Text => [],
        LineBreak => [],
        JumpIndicator => [],
        DefaultSpeaker => [],
        SpeakerNameReference => [],
        SpeakerIdReference => [],
        Query => [],
        DefaultCommand => [],
        CustomCommand => [],
        CustomTag => [],
        ReservedTag => [],

        _ => throw new ArgumentOutOfRangeException(
            nameof(node), node.GetType(), "Unhandled script node type in Children()."),
    };

    /// <summary>
    /// The node's concrete type and each base up to and including <see cref="ScriptNode"/>
    /// (object is excluded). Filing a node under every type in this chain lets a base-type
    /// query such as <c>OfType&lt;Speaker&gt;()</c> find it.
    /// </summary>
    internal static IEnumerable<Type> TypeChainToScriptNode(this ScriptNode node)
    {
        // The walk stops at ScriptNode — object fails the assignability check — so
        // BaseType is never null while looping.
        for (Type type = node.GetType();
            typeof(ScriptNode).IsAssignableFrom(type);
            type = type.BaseType!)
        {
            yield return type;
        }
    }

    // A line's speaker (when present) comes before its speech, so traversal keeps
    // document order.
    private static IEnumerable<ScriptNode> LineChildren(Line line)
    {
        if (line.Speaker is not null)
        {
            yield return line.Speaker;
        }

        foreach (var fragment in line.Speech)
        {
            yield return fragment;
        }
    }
}
