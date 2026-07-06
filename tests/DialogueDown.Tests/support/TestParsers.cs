using DialogueDown.Script.Transpiler.Parsing;
using Superpower;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Ready-made leaf parsers for tests. Centralized so the same grammar isn't
/// re-declared across parser test files.
/// </summary>
internal static class TestParsers
{
    /// <summary>A C-style identifier leaf, the common building block in parser tests.</summary>
    public static IParser<string> Identifier { get; } =
        SuperpowerParser.Wrap(
            Superpower.Parsers.Identifier.CStyle.Select(name => name.ToStringValue()));
}
