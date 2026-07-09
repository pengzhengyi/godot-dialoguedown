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
        var watchOption = new Option<bool>("--watch")
        {
            Description = "Serve the report from a local server and hot-reload it on file changes.",
        };
        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Write the report to this path instead of a temp file (static mode).",
        };
        var portOption = new Option<int?>("--port")
        {
            Description = "The loopback port for --watch (default: an ephemeral port).",
        };
        var noOpenOption = new Option<bool>("--no-open")
        {
            Description = "Do not open the report in the browser.",
        };

        var root = new RootCommand("Visualize a DialogueDown script's compilation.")
        {
            fileArgument,
            watchOption,
            outputOption,
            portOption,
            noOpenOption,
        };

        root.SetAction((parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(fileArgument)!;
            var noOpen = parseResult.GetValue(noOpenOption);
            if (parseResult.GetValue(watchOption))
            {
                return WatchMode.RunAsync(
                    file,
                    parseResult.GetValue(portOption),
                    noOpen,
                    browser,
                    Console.Out,
                    Console.Error,
                    cancellationToken);
            }

            return Task.FromResult(StaticMode.Run(
                file,
                parseResult.GetValue(outputOption),
                noOpen,
                browser,
                Console.Error));
        });

        return root;
    }
}
