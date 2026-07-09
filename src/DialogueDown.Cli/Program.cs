using System.Diagnostics.CodeAnalysis;
using DialogueDown.Cli;

[ExcludeFromCodeCoverage] // The composition root: delegates to the runner.
internal static class Program
{
    private static int Main(string[] args) => CliRunner.Run(args);
}

