namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void EveryDescriptorHasAUniqueCode()
    {
        var codes = DiagnosticCatalogReflection.Descriptors().Select(descriptor => descriptor.Code).ToList();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }
}
