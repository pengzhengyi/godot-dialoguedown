using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>
/// A Dialogue AST block of a type the rewriter does not model, for exercising the default
/// branch of a block dispatch (the "unknown kind" path).
/// </summary>
internal sealed record UnknownScriptBlock(SourceSpan Span) : ScriptBlock(Span);
