namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The parsed form of a tag: whether it is reserved (<c>##</c>) or custom (<c>#</c>),
/// its name, and an optional group value. Span-free — the builder attaches the span.
/// </summary>
internal sealed record TagData(bool IsReserved, string Name, string? Value);
