using Spectre.Console.Cli;

namespace DialogueDown.Cli.Infrastructure;

/// <summary>Resolves types for Spectre.Console.Cli from a built service provider.</summary>
internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <inheritdoc />
    public object? Resolve(Type? type) => type is null ? null : _provider.GetService(type);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
