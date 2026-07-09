using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsed;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a tag node from parsed <see cref="TagData"/>: a reserved (<c>##</c>) tag
/// becomes a <see cref="ReservedTag"/> and a custom (<c>#</c>) tag a
/// <see cref="CustomTag"/>, each stamped with the given source span.
/// </summary>
internal sealed class TagBuilder
{
    public Tag Build(TagData data, SourceSpan span) =>
        data.IsReserved
            ? new ReservedTag(data.Name, data.Value, span)
            : new CustomTag(data.Name, data.Value, span);
}
