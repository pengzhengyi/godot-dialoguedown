using System.CommandLine;

namespace DialogueDown.Visualization.Live;

/// <summary>Builds the <c>visualize</c> command-line interface.</summary>
internal static class VisualizeCli
{
    /// <summary>
    /// Creates the root command. <paramref name="browser"/> is injected so tests can
    /// verify the open-in-browser behavior without launching one.
    /// </summary>
    public static RootCommand Create(IBrowserLauncher browser)
    {
        var fileArgument = new Argument<string>("file")
        {
            Description = "The .dialogue.md script to visualize.",
        };
        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Write the report to this path instead of a temp file.",
        };
        var noOpenOption = new Option<bool>("--no-open")
        {
            Description = "Do not open the report in the browser.",
        };

        var root = new RootCommand("Visualize a DialogueDown script's compilation.")
        {
            fileArgument,
            outputOption,
            noOpenOption,
        };

        root.SetAction(parseResult => StaticMode.Run(
            parseResult.GetValue(fileArgument)!,
            parseResult.GetValue(outputOption),
            parseResult.GetValue(noOpenOption),
            browser,
            Console.Error));

        return root;
    }
}
