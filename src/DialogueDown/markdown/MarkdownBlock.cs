using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// Base type for block-level Markdown content — the top-level pieces of a
/// document such as headings, paragraphs, and lists.
/// </summary>
internal abstract record MarkdownBlock(SourceSpan Span);
