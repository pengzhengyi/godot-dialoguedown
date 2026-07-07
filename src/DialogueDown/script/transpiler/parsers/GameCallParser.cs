using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;
using Superpower;
using Superpower.Parsers;

namespace DialogueDown.Script.Transpiler.Parsers;

/// <summary>
/// The grammar that recognizes the inner text of a code span as a game call and
/// reports it as <see cref="GameCallData"/>: a query, a default command, or a named
/// command with arguments. It only recognizes shape; a separate builder makes the
/// AST node and reports text that is not a game call.
/// </summary>
internal static class GameCallParser
{
    // A comma between arguments may be padded with whitespace on either side, so
    // JoinClub("Alice" , "Art") parses the same as JoinClub("Alice", "Art").
    private static readonly TextParser<char> _argumentSeparator =
        from leading in Character.WhiteSpace.Many()
        from comma in Character.EqualTo(',')
        from trailing in Character.WhiteSpace.Many()
        select comma;

    private static readonly TextParser<GameCallData> _query =
        SuperpowerPrimitives.QuotedString.Select(key => (GameCallData)new QueryData(key));

    private static readonly TextParser<GameCallData> _defaultCommand =
        from action in SuperpowerPrimitives.QuotedString.EnclosedInParentheses()
        select (GameCallData)new DefaultCommandData(action);

    private static readonly TextParser<GameCallData> _customCommand =
        from name in Identifier.CStyle
        from args in SuperpowerPrimitives.QuotedString
            .ManyDelimitedBy(_argumentSeparator)
            .EnclosedInParentheses()
        select (GameCallData)new CustomCommandData(name.ToStringValue(), args);

    public static IParser<GameCallData> Grammar { get; } = SuperpowerParser.Wrap(
        _query.Try()
            .Or(_defaultCommand.Try())
            .Or(_customCommand));
}
