using System.Reflection;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void EveryDescriptorHasAUniqueCode()
    {
        var codes = Descriptors().Select(descriptor => descriptor.Code).ToList();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    // Reflects over the catalog's public static DiagnosticDescriptor fields, so the uniqueness check
    // covers every entry without the test naming them one by one.
    private static IEnumerable<DiagnosticDescriptor> Descriptors() =>
        typeof(DiagnosticCatalog)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => (DiagnosticDescriptor)field.GetValue(null)!);
}
