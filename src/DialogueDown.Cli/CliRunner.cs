using DialogueDown.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DialogueDown.Cli;

/// <summary>
/// Composition root: wire the services, build the Spectre command app, and run it.
/// Kept separate from <c>Program</c> so the wiring is a normal, callable method.
/// </summary>
internal static class CliRunner
{
    /// <summary>Runs the CLI for the given arguments and returns a process exit code.</summary>
    public static int Run(string[] args)
    {
        var services = new ServiceCollection();
        CliServices.Register(services);
        var registrar = new TypeRegistrar(services);

        var app = new CommandApp(registrar);
        app.Configure(CliConfigurator.Configure);
        return app.Run(args);
    }
}
