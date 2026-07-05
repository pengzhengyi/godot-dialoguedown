using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A built-in tag owned by DialogueDown that drives language behavior. Only a
/// known set is valid, so an unrecognized reserved tag is rejected later.
/// </summary>
internal sealed record ReservedTag(string Name, string? Value, SourceSpan Span)
    : Tag(Name, Value, Span);
