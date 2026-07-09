using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DialogueDown.Cli;

/// <summary>Registers the CLI's services for dependency injection.</summary>
internal static class CliServices
{
    /// <summary>Adds the CLI's collaborators to <paramref name="services"/> and returns it.</summary>
    public static IServiceCollection Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(AnsiConsole.Console);
        return services;
    }
}
