using DialogueDown.Cli.Compilation;
using DialogueDown.Cli.Infrastructure;
using DialogueDown.Visualization.Live;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli.Testing;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>
/// Builds a <see cref="CommandAppTester"/> wired exactly like production
/// (<see cref="CliServices"/> + <see cref="CliConfigurator"/>), optionally
/// substituting the script compiler or the visualize runner so command behavior can
/// be tested in isolation.
/// </summary>
internal static class CliTester
{
    /// <summary>
    /// Creates a tester. A given <paramref name="compiler"/> or
    /// <paramref name="runner"/> replaces the default registration (last wins).
    /// </summary>
    public static CommandAppTester Create(
        IScriptCompiler? compiler = null,
        IVisualizeRunner? runner = null,
        ILauncherRunner? launcher = null)
    {
        var services = new ServiceCollection();
        CliServices.Register(services);
        if (compiler is not null)
        {
            services.AddSingleton(compiler);
        }

        if (runner is not null)
        {
            services.AddSingleton(runner);
        }

        if (launcher is not null)
        {
            services.AddSingleton(launcher);
        }

        var tester = new CommandAppTester(new TypeRegistrar(services));
        tester.Configure(CliConfigurator.Configure);
        return tester;
    }
}
