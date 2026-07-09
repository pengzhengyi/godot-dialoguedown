using DialogueDown.Cli.Compilation;
using Microsoft.Extensions.DependencyInjection;

namespace DialogueDown.Cli;

/// <summary>Registers the CLI's services for dependency injection.</summary>
internal static class CliServices
{
    /// <summary>Adds the CLI's collaborators to <paramref name="services"/> and returns it.</summary>
    public static IServiceCollection Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IScriptCompiler, PendingScriptCompiler>();
        return services;
    }
}
