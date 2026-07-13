using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration extensions that add the DialogueDown compiler and its stages to a
/// dependency injection container.
/// </summary>
public static class DialogueDownServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default compiler stages and the <see cref="IScriptCompiler"/> facade
    /// as singletons. Each stage is registered with <c>TryAdd</c>, so registering your own
    /// implementation of a stage before calling this swaps just that stage while the rest
    /// keep their defaults.
    /// </summary>
    public static IServiceCollection AddDialogueDown(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMarkdownParser>(_ => new MarkdigMarkdownParser());
        services.TryAddSingleton<IScriptTranspiler>(_ => ScriptTranspilerFactory.CreateDefault());
        services.TryAddSingleton<IScriptDesugarer, ScriptDesugarer>();
        services.TryAddSingleton<ISemanticAnalyzer, SemanticAnalyzer>();
        services.TryAddSingleton<IScriptCompiler>(provider => new ScriptCompiler(
            provider.GetRequiredService<IMarkdownParser>(),
            provider.GetRequiredService<IScriptTranspiler>(),
            provider.GetRequiredService<IScriptDesugarer>(),
            provider.GetRequiredService<ISemanticAnalyzer>()));

        return services;
    }
}
