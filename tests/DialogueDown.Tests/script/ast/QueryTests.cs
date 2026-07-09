using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class QueryTests
{
    [Fact]
    public void Constructor_ExposesKeyAndSpan_AndIsAGameCall()
    {
        var span = SourceSpanFactory.Span();

        var query = new Query("Alice.FavoriteColor", span);

        Assert.Equal("Alice.FavoriteColor", query.Key);
        Assert.Equal(span, query.Span);
        Assert.IsAssignableFrom<GameCall>(query);
        Assert.IsAssignableFrom<InlineFragment>(query);
        Assert.IsAssignableFrom<ScriptNode>(query);
    }
}
