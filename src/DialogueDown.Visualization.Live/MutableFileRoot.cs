using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A static-file provider whose backing directory can change at runtime — the launcher's
/// active root. Each open re-points it at the chosen root (<see cref="Set"/>) so the
/// report's relative assets resolve there, without rebuilding the middleware. Serves
/// nothing until a root is set.
/// </summary>
internal sealed class MutableFileRoot : IFileProvider
{
    private volatile PhysicalFileProvider? _current;

    /// <summary>Points the provider at <paramref name="directory"/> as the active root.</summary>
    public void Set(string directory) => _current = new PhysicalFileProvider(directory);

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath) =>
        _current?.GetFileInfo(subpath) ?? new NotFoundFileInfo(subpath);

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath) =>
        _current?.GetDirectoryContents(subpath) ?? NotFoundDirectoryContents.Singleton;

    /// <inheritdoc />
    public IChangeToken Watch(string filter) =>
        _current?.Watch(filter) ?? NullChangeToken.Singleton;
}
