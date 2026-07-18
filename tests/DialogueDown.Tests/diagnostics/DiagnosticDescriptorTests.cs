using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticDescriptorTests
{
    [Fact]
    public void Constructor_CodeMatchingCategory_ExposesAllFields()
    {
        var descriptor = new DiagnosticDescriptor(
            "DLG2001",
            "A title",
            "A message '{0}'.",
            DiagnosticCategory.Semantic,
            DiagnosticSeverity.Warning);

        Assert.Equal("DLG2001", descriptor.Code);
        Assert.Equal("A title", descriptor.Title);
        Assert.Equal("A message '{0}'.", descriptor.MessageFormat);
        Assert.Equal(DiagnosticCategory.Semantic, descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
    }

    [Fact]
    public void Constructor_EachCategoryAcceptsItsOwnCodeRange()
    {
        Assert.Equal(
            DiagnosticCategory.Syntax,
            Descriptor("DLG1001", DiagnosticCategory.Syntax).Category);
        Assert.Equal(
            DiagnosticCategory.Semantic,
            Descriptor("DLG2001", DiagnosticCategory.Semantic).Category);
        Assert.Equal(
            DiagnosticCategory.Style,
            Descriptor("DLG3001", DiagnosticCategory.Style).Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DLG1")] // too short
    [InlineData("DLG12345")] // too long
    [InlineData("XYZ1001")] // wrong prefix
    [InlineData("dlg1001")] // lowercase prefix
    [InlineData("DLG10A1")] // a non-digit in the number
    public void Constructor_MalformedCode_ThrowsArgumentException(string? code)
    {
        Assert.Throws<ArgumentException>(() => Descriptor(code!, DiagnosticCategory.Syntax));
    }

    [Fact]
    public void Constructor_CodeRangeMismatchesCategory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Descriptor("DLG2001", DiagnosticCategory.Syntax));
        Assert.Throws<ArgumentException>(() => Descriptor("DLG1001", DiagnosticCategory.Semantic));
        Assert.Throws<ArgumentException>(() => Descriptor("DLG1001", DiagnosticCategory.Style));
    }

    [Fact]
    public void Constructor_UnknownCategory_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Descriptor("DLG1001", (DiagnosticCategory)999));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        Assert.Equal(
            Descriptor("DLG1001", DiagnosticCategory.Syntax),
            Descriptor("DLG1001", DiagnosticCategory.Syntax));
    }

    private static DiagnosticDescriptor Descriptor(string code, DiagnosticCategory category) =>
        new(code, "Title", "Message.", category, DiagnosticSeverity.Error);
}
