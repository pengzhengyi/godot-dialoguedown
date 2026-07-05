using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A command carrying a single free-text instruction that is passed to the game
/// system to run. Contrast <see cref="CustomCommand"/>, which names a specific
/// operation and its arguments.
/// </summary>
internal sealed record DefaultCommand(string Text, SourceSpan Span) : GameCall(Span);
