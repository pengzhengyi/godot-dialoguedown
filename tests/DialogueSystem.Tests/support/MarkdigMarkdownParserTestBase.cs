using DialogueSystem.Markdown;

namespace DialogueSystem.Tests.Support;

/// <summary>
/// Base for parser test classes: provides one shared parser instance so each
/// feature-focused test class does not repeat the setup.
/// </summary>
public abstract class MarkdigMarkdownParserTestBase
{
    private protected IMarkdownParser Parser { get; } = new MarkdigMarkdownParser();
}
