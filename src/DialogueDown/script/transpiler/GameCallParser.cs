using DialogueDown.Common;
using DialogueDown.Script.Ast;
using Superpower;
using Superpower.Parsers;
using GameCallFromSpan =
    System.Func<DialogueDown.Common.SourceSpan, DialogueDown.Script.Ast.GameCall>;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Parses the inner text of a code span into a <see cref="GameCall"/>: a query, a
/// default command, or a named command with arguments. Text that matches none of
/// these is rejected with a <see cref="DialogueSyntaxError"/>.
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

    private static readonly TextParser<GameCallFromSpan> _queryParser =
        ParserPrimitives.QuotedString.Select(
            key => (GameCallFromSpan)(span => new Query(key, span)));

    private static readonly TextParser<GameCallFromSpan> _defaultCommandParser =
        from action in ParserPrimitives.QuotedString.EnclosedInParentheses()
        select (GameCallFromSpan)(span => new DefaultCommand(action, span));

    private static readonly TextParser<GameCallFromSpan> _customCommandParser =
        from name in Identifier.CStyle
        from args in ParserPrimitives.QuotedString
            .ManyDelimitedBy(_argumentSeparator)
            .EnclosedInParentheses()
        select (GameCallFromSpan)(span => new CustomCommand(name.ToStringValue(), args, span));

    private static readonly TextParser<GameCallFromSpan> _grammar =
        _queryParser.Try()
            .Or(_defaultCommandParser.Try())
            .Or(_customCommandParser)
            .AtEnd();

    public static GameCall Parse(string content, SourceSpan span)
    {
        try
        {
            return _grammar.Parse(content)(span);
        }
        catch (ParseException error)
        {
            // Keep Superpower's precise message as the inner exception while the
            // author sees our own, friendlier explanation.
            throw new DialogueSyntaxError(BuildMessage(content), span, error);
        }
    }

    private static string BuildMessage(string content) =>
        $"""
        "{content}" is not a game call. Acceptable forms are:
          - a query that reads a value: "key"
          - a default command: ("do something")
          - a named command: Name("arg", ...)
        """;
}
