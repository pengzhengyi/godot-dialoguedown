using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group A — assembly boundaries. The dependency direction is
/// <c>Cli -> Visualization.Live -> Visualization -> Core</c>, and it must never
/// reverse: lower layers stay unaware of the layers built on top of them.
/// </summary>
public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void Core_DoesNotDependOn_CliVisualizationOrLive()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Cli,
                Architecture.Visualization,
                Architecture.VisualizationLive)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Core_DoesNotDependOn_PresentationOrHostLibraries()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.SpectreConsole,
                Architecture.Godot,
                Architecture.SystemConsole)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Visualization_DoesNotDependOn_CliOrLive()
    {
        Types.InAssembly(Architecture.VisualizationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Cli,
                Architecture.VisualizationLive)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void VisualizationLive_DoesNotDependOn_Cli()
    {
        Types.InAssembly(Architecture.VisualizationLiveAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Architecture.Cli)
            .GetResult()
            .ShouldPass();
    }
}
