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

    [Fact]
    public void Transact_MissingDirectory_ReportsNullWithoutWriting()
    {
        // The containing directory does not exist: the snapshot read reports a missing file (null)
        // rather than throwing, and a no-write body leaves nothing behind.
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "nope", "doc.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Transact_MissingFileCreatedConcurrently_IsReadAndNeverDeleted()
    {
        // Simulate the existence race: the target does not exist when the transaction begins, but
        // an external writer creates it while the body runs. A no-write outcome must never delete
        // that externally created file (the old File.Exists placeholder-and-delete bug).
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "raced.txt");

        var seen = AtomicFile.Transact(path, transaction =>
        {
            File.WriteAllText(path, "external content"); // appears mid-transaction
            return transaction.Disk;
        });

        Assert.Null(seen); // it did not exist when the snapshot was read
        Assert.True(File.Exists(path));
        Assert.Equal("external content", File.ReadAllText(path)); // never deleted or clobbered
    }

    [Fact]
    public void Transact_Write_LeavesNoStagingTempBehind()
    {
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        // The staged bytes move atomically into place; no leftover temp file remains beside it.
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_Write_StagesInTheSameDirectory()
    {
        // The staging temp must be a sibling of the target so the final move stays on one volume
        // (an atomic rename), not a cross-device copy through the system temp folder.
        using var tree = new Support.TempTree();
        var directory = tree.Dir("nested");
        var path = Path.Combine(directory, "doc.txt");
        File.WriteAllText(path, "original");

        string? stagingDirectory = null;
        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            stagingDirectory = Directory.GetFiles(directory)
                .FirstOrDefault(file => !string.Equals(file, path, StringComparison.Ordinal)) is { } temp
                ? Path.GetDirectoryName(temp)
                : null;
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        if (stagingDirectory is not null)
        {
            Assert.Equal(directory, stagingDirectory);
        }
    }

    [Fact]
    public void Transact_WriteThatFailsToStage_PreservesTheOriginalUnchanged()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // directory permission bits do not block file creation the same way on Windows
        }

        using var tree = new Support.TempTree();
        var directory = tree.Dir("locked");
        var path = Path.Combine(directory, "doc.txt");
        File.WriteAllText(path, "original");

        // Make the directory read-only so staging a sibling temp file fails; the original must be
        // left byte-for-byte intact rather than truncated or partially written.
        var info = new DirectoryInfo(directory);
        var mode = File.GetUnixFileMode(directory);
        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.ThrowsAny<Exception>(() => AtomicFile.Transact(path, transaction =>
            {
                transaction.Write("replacement that must never land");
                return 0;
            }));
            Assert.Equal("original", File.ReadAllText(path));
        }
        finally
        {
            File.SetUnixFileMode(directory, mode);
            _ = info;
        }
    }
}
