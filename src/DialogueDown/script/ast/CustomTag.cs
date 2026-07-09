using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A project-defined tag carrying arbitrary metadata that tools and the runtime
/// may interpret. Its meaning is open — DialogueDown passes it through.
/// </summary>
internal sealed record CustomTag(string Name, string? Value, SourceSpan Span)
    : Tag(Name, Value, Span);
