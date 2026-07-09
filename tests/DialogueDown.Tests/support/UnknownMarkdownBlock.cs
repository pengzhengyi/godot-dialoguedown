using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// A Markdown block of a type the transpiler does not model, for exercising the
/// default branch of a block switch (the "unhandled kind" path).
/// </summary>
internal sealed record UnknownMarkdownBlock(SourceSpan Span) : MarkdownBlock(Span);
