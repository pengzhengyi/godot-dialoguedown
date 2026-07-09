using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DialogueDown.Cli.Infrastructure;

/// <summary>
/// Bridges Spectre.Console.Cli's <see cref="ITypeRegistrar"/> onto
/// <see cref="Microsoft.Extensions.DependencyInjection"/>, so commands receive their
/// collaborators by constructor injection and tests can substitute them.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <inheritdoc />
    public ITypeResolver Build() => new TypeResolver(_services.BuildServiceProvider());

    /// <inheritdoc />
    public void Register(Type service, Type implementation) =>
        _services.AddSingleton(service, implementation);

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) =>
        _services.AddSingleton(service, implementation);

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _services.AddSingleton(service, _ => factory());
    }
}
