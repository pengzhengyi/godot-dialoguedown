using DialogueSystem.Markdown;
using Ast = DialogueSystem.Tests.Support.MarkdownAstFactory;

namespace DialogueSystem.Tests.Markdown;

public sealed class CodeSpanInlineTests
{
    [Fact]
    public void Constructor_ExposesContentAndSpan_AndIsInline()
    {
        var span = Ast.Span();

        var inline = new CodeSpanInline("\"Alice.FavoriteColor\"", span);

        Assert.Equal("\"Alice.FavoriteColor\"", inline.Content);
        Assert.Equal(span, inline.Span);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }
}
