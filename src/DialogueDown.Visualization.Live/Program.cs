using System.Diagnostics.CodeAnalysis;
using DialogueDown.Visualization.Live;

/// <summary>The <c>visualize</c> command-line entry point (composition root).</summary>
[ExcludeFromCodeCoverage] // The composition root: wires the real browser launcher and delegates.
internal static class Program
{
    private static int Main(string[] args) =>
        VisualizeCli.Create(new BrowserLauncher()).Parse(args).Invoke();
}
