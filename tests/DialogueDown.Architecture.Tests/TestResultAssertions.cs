using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Turns a <see cref="TestResult"/> into an xUnit assertion whose failure message
/// names every offending type, so a broken boundary points straight at its cause.
/// </summary>
internal static class TestResultAssertions
{
    public static void ShouldPass(this TestResult result)
    {
        Assert.True(result.IsSuccessful, DescribeFailure(result));
    }

    private static string DescribeFailure(TestResult result)
    {
        var offenders = result.FailingTypes ?? [];
        return "Architecture rule violated by:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders.Select(type => "  - " + type.FullName));
    }
}
