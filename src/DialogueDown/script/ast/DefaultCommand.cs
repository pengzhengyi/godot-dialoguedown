using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A command carrying a single action for the game system to run. Contrast
/// <see cref="CustomCommand"/>, which names a specific operation and its
/// arguments.
/// </summary>
internal sealed record DefaultCommand(string Action, SourceSpan Span) : GameCall(Span);
