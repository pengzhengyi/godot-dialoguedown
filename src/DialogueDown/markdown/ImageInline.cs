using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown image (<c>![alt](src)</c>). <see cref="Source"/> is where the image
/// is loaded from and <see cref="AltText"/> is its alternative text; both are kept
/// exactly as written. A presentation adapter can render it inline in a chat (for
/// example a portrait or emoji between words). Modeled like <see cref="LinkInline"/>.
/// </summary>
internal sealed record ImageInline(string Source, string AltText, SourceSpan Span)
    : MarkdownInline(Span);
