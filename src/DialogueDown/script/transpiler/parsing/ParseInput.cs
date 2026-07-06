namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Input to a parser: the <see cref="Text"/> to read, and the absolute
/// <see cref="Position"/> that its first character occupies in the source.
/// Parsing always begins at the start of <see cref="Text"/>; <see cref="Position"/>
/// is only an anchor, so matches report their range in absolute source coordinates.
/// </summary>
internal readonly record struct ParseInput(string Text, int Position)
{
    /// <summary>The full range this input covers in the source.</summary>
    public TextRange Range => new(Position, Text.Length);
}
