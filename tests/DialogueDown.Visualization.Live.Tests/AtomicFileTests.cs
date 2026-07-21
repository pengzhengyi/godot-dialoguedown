using System.Text;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class AtomicFileTests
{
    [Fact]
    public void Transact_ReadsTheCurrentContentUnderTheLock()
    {
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "on disk");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Equal("on disk", seen);
    }

    [Fact]
    public void Transact_MissingFile_ReportsNullAndLeavesNoFileWhenNotWritten()
    {
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "missing.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(File.Exists(path)); // the inspection placeholder is removed on a no-write outcome
    }

    [Fact]
    public void Transact_Write_CommitsContentWithoutABom()
    {
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "created.txt");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        Assert.False(File.ReadAllBytes(path).Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
    }

    [Fact]
    public void Transact_Write_TruncatesAShorterReplacement()
    {
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "a long original line");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("short");
            return 0;
        });

        Assert.Equal("short", File.ReadAllText(path));
    }
}
