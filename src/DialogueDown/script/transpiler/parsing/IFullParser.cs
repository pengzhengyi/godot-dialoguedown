namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A parser that consumes an <em>entire</em> input into a value. Used where the text
/// is expected to be one complete thing — for example the inner text of a code span
/// parsed as a game call or a tag. Failure or leftover text raises a
/// <see cref="DialogueSyntaxError"/>.
/// </summary>
internal interface IFullParser<T>
{
    T ParseAll(ParseInput input);
}
