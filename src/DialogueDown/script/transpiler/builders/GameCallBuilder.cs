using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Errors;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a <see cref="GameCall"/> from a code span's inner text. The whole text must
/// be one game call: text that is not raises a <see cref="DialogueSyntaxError"/> with
/// a friendly explanation plus the grammar's technical reason. The node carries the
/// code span's source span.
/// </summary>
internal sealed class GameCallBuilder(IParser<GameCallData> parser)
{
    public GameCall Build(ParseInput input, SourceSpan span)
    {
        var result = parser.ConsumeAll(input);
        if (result.Success)
        {
            return ToNode(result.MatchedValue, span);
        }

        throw new DialogueSyntaxError(result.Explain(NotAGameCallMessage(input.Text)), span);
    }

    private static GameCall ToNode(GameCallData data, SourceSpan span)
    {
        if (data is QueryData query)
        {
            return new Query(query.Key, span);
        }

        if (data is DefaultCommandData defaultCommand)
        {
            return new DefaultCommand(defaultCommand.Action, span);
        }

        var custom = (CustomCommandData)data;
        return new CustomCommand(custom.Name, custom.Args, span);
    }

    private static string NotAGameCallMessage(string content) =>
        $"""
        "{content}" is not a game call. Acceptable forms are:
          - a query that reads a value: "key"
          - a default command: ("do something")
          - a named command: Name("arg", ...)
        """;
}
