using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds <see cref="ParseInput"/> values for tests, defaulting the anchor
/// <see cref="ParseInput.Position"/> to zero so most cases read as just the text.
/// </summary>
internal static class ParseInputFactory
{
    public static ParseInput Input(string text, int position = 0) => new(text, position);
}
