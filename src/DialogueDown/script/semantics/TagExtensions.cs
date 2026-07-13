using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Semantic helpers over AST tags, used when a stage compares tags by meaning rather than
/// by where they sit in the source.
/// </summary>
internal static class TagExtensions
{
    /// <summary>
    /// A tag's semantic identity: its name and value, ignoring the source span. Two tags
    /// with the same key are the same tag as far as a speaker's merged tags are concerned.
    /// </summary>
    internal static (string Name, string? Value) SemanticKey(this Tag tag) => (tag.Name, tag.Value);
}
