using DialogueDown.Cli.Compilation;
using DialogueDown.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli.Testing;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>
/// Builds a <see cref="CommandAppTester"/> wired exactly like production
/// (<see cref="CliServices"/> + <see cref="CliConfigurator"/>), optionally
/// substituting the script compiler so command behavior can be tested in isolation.
/// </summary>
internal static class CliTester
{
    /// <summary>
    /// Creates a tester. When <paramref name="compiler"/> is given it replaces the
    /// default <see cref="IScriptCompiler"/> registration (last registration wins).
    /// </summary>
    public static CommandAppTester Create(IScriptCompiler? compiler = null)
    {
        var services = new ServiceCollection();
        CliServices.Register(services);
        if (compiler is not null)
        {
            services.AddSingleton(compiler);
        }

        var tester = new CommandAppTester(new TypeRegistrar(services));
        tester.Configure(CliConfigurator.Configure);
        return tester;
    }
}
