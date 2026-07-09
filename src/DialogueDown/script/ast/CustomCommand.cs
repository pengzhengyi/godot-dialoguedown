using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A command that names a specific operation and its arguments, run against the
/// game system. Contrast <see cref="DefaultCommand"/>, which carries a single
/// free-text instruction.
/// </summary>
internal sealed record CustomCommand(string Name, IReadOnlyList<string> Args, SourceSpan Span)
    : GameCall(Span);
