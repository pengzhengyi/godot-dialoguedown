using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group C — convention hygiene. Keeps the error vocabulary discoverable: the
/// thrown exception hierarchy lives together under <c>*.Errors</c>, while value
/// types that merely describe a failure (for example a parse-failure record) are
/// deliberately excluded because they are data, not exceptions.
/// </summary>
public sealed class ConventionTests
{
    [Fact]
    public void ExceptionTypes_ResideIn_AnErrorsNamespace()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .ResideInNamespaceEndingWith(".Errors")
            .GetResult()
            .ShouldPass();
    }
}
