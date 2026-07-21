using System.Text;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A read → decide → write window over one file that never destroys data. A caller reads the
/// file's current content (a snapshot taken by an atomic open, so a missing file is reported as
/// <c>null</c> without a stale <see cref="File.Exists"/> probe), runs its baseline/validation
/// checks, and optionally stages a full replacement. A staged write is buffered in full, written
/// to a same-directory temporary file, flushed to disk, and then moved atomically over the target,
/// so a failure at any point leaves the original file byte-for-byte intact and never creates or
/// deletes an externally owned file.
/// </summary>
/// <remarks>
/// Writes to the same path are serialized within this process by a per-path lock, so two
/// concurrent in-process saves from the same baseline settle as one write and one conflict rather
/// than a lost update. Against a <em>separate</em> process (an external editor) this is
/// <em>optimistic</em> concurrency, not a held OS lock: the snapshot read and the atomic replace
/// are distinct steps, which keeps the replace a portable, cross-volume-safe rename. That narrow
/// window is covered by the caller's expected-baseline check plus the on-disk watcher (an external
/// change is reported as a conflict), and the atomic move guarantees a reader ever sees only the
/// whole old file or the whole new file — never a partial write.
/// </remarks>
internal static class AtomicFile
{
    // A brief bounded spin lets a concurrent writer (another save, or a quick external editor)
    // release its handle so the atomic replace can complete instead of failing the save outright.
    private const int MaxAttempts = 100;

    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(5);

    // One lock per target path serializes this process's own read→decide→write windows, so two
    // concurrent in-process saves from the same baseline settle as one write and one conflict
    // rather than a lost update. It does not serialize a separate external process — that narrower,
    // portable guarantee is covered by the baseline check and the watcher (see the type remarks).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _locks =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Reads the current content of <paramref name="path"/> and invokes <paramref name="body"/>
    /// with a <see cref="Transaction"/> exposing that content and a <see cref="Transaction.Write"/>
    /// that stages a full replacement. A file that does not exist is reported as <c>null</c>
    /// content; a no-write outcome touches nothing on disk, so a file that was never present stays
    /// absent and an externally created one is left untouched. A staged write is committed by an
    /// atomic same-directory move after <paramref name="body"/> returns, so the original is never
    /// truncated in place and a failed write preserves it.
    /// </summary>
    public static T Transact<T>(string path, Func<Transaction, T> body)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        lock (LockFor(path))
        {
            var disk = ReadSnapshot(path);
            var transaction = new Transaction(disk);
            var result = body(transaction);

            if (transaction.Wrote)
            {
                Commit(path, transaction.PendingContent);
            }

            return result;
        }
    }

    /// <summary>
    /// Creates <paramref name="path"/> from <paramref name="content"/> only when it does not
    /// already exist: the full bytes are staged in a same-directory temporary file, flushed, then
    /// moved into place with a no-overwrite atomic move, so a concurrent create loses the race and
    /// an incomplete temp is removed on any failure. Throws <see cref="IOException"/> when a file
    /// already exists at <paramref name="path"/> (the target is left untouched), matching the
    /// exclusive-create contract callers detect a conflict by.
    /// </summary>
    public static void CreateNew(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        lock (LockFor(fullPath))
        {
            try
            {
                using (var stream = new FileStream(
                    temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, _utf8NoBom, leaveOpen: true))
                    {
                        writer.Write(content);
                        writer.Flush();
                    }

                    stream.Flush(flushToDisk: true);
                }

                // No-overwrite: a file already at the target makes this throw, so the existing file is
                // never clobbered and the caller recognizes the conflict.
                File.Move(temp, fullPath, overwrite: false);
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }
    }

    private static object LockFor(string path) =>
        _locks.GetOrAdd(Path.GetFullPath(path), _ => new object());

    // Reads the file's content, or null when it does not exist. Existence is established by the
    // open itself (a FileNotFound/DirectoryNotFound throw) rather than a separate File.Exists probe
    // that could race a concurrent create or delete.
    private static string? ReadSnapshot(string path)
    {
        try
        {
            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(
                stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    // Stages the complete replacement bytes in a same-directory temporary file, flushes them to
    // disk, then moves the temp over the target in one atomic step. The original is untouched until
    // that move, so any failure while staging leaves it intact; an incomplete temp is always
    // removed. Guaranteeing a same-volume sibling keeps the move a rename, not a copy.
    private static void Commit(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, _utf8NoBom, leaveOpen: true))
                {
                    writer.Write(content);
                    writer.Flush();
                }

                stream.Flush(flushToDisk: true);
            }

            Replace(temp, fullPath);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    // Atomically moves the staged temp over the target, retrying briefly so a transient handle held
    // by another writer does not fail the save. Replacing an existing file and creating a new one
    // are one operation (overwrite), so the target's existence never has to be probed first.
    private static void Replace(string temp, string target)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temp, target, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(_retryDelay);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort: a leftover uniquely named temp is harmless and never the target file.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort: see above.
        }
    }

    /// <summary>
    /// One read → decide → write transaction: the <see cref="Disk"/> content snapshot, and a
    /// <see cref="Write"/> that stages a full replacement committed atomically after the body
    /// returns.
    /// </summary>
    internal sealed class Transaction
    {
        internal Transaction(string? disk) => Disk = disk;

        /// <summary>The file's content when the transaction began, or <c>null</c> if it did not exist.</summary>
        public string? Disk { get; }

        /// <summary>Whether <see cref="Write"/> staged content in this transaction.</summary>
        internal bool Wrote { get; private set; }

        /// <summary>The staged replacement content, valid only when <see cref="Wrote"/> is <c>true</c>.</summary>
        internal string PendingContent { get; private set; } = string.Empty;

        /// <summary>Stages <paramref name="content"/> as the file's full replacement, committed atomically on return.</summary>
        public void Write(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            PendingContent = content;
            Wrote = true;
        }
    }
}
