using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// A Markdown inline of a type the transpiler does not model, for exercising the
/// default branch of an inline switch (the "unhandled kind" path).
/// </summary>
internal sealed record UnknownMarkdownInline(SourceSpan Span) : MarkdownInline(Span);
