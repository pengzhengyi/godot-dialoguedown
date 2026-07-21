using System.Text;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A single exclusive read/compare/write window over one file. A caller runs its baseline check,
/// optional validation, and write inside one <see cref="Transact{T}"/> call while holding an
/// exclusive handle, so no external writer can change the file between the check and the commit —
/// the compare and the write are one atomic step, not two racing operations.
/// </summary>
internal static class AtomicFile
{
    // A brief bounded spin lets a concurrent writer (another save, or a quick external editor)
    // release its exclusive handle instead of failing the save outright.
    private const int MaxAttempts = 100;

    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(5);

    /// <summary>
    /// Opens <paramref name="path"/> exclusively, reads its current content, and invokes
    /// <paramref name="body"/> with a <see cref="Transaction"/> that exposes the content and a
    /// <see cref="Transaction.Write"/> that commits new content through the same handle. The whole
    /// compare→decide→write sequence runs under the one lock, so the content
    /// <paramref name="body"/> inspects is exactly the content a later <see cref="Transaction.Write"/>
    /// replaces. A file that does not exist is reported as a <c>null</c> content and is created only
    /// if <paramref name="body"/> writes; a no-write outcome leaves it absent.
    /// </summary>
    public static T Transact<T>(string path, Func<Transaction, T> body)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        for (var attempt = 1; ; attempt++)
        {
            var existed = File.Exists(path);
            FileStream stream;
            try
            {
                stream = new FileStream(
                    path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(_retryDelay);
                continue;
            }

            T result;
            bool wrote;
            using (stream)
            {
                var transaction = new Transaction(stream, existed ? ReadAll(stream) : null);
                result = body(transaction);
                wrote = transaction.Wrote;
            }

            // The file was created just to inspect a missing target, but the caller chose not to
            // write; remove the empty placeholder so a no-write outcome leaves no file behind.
            if (!existed && !wrote)
            {
                File.Delete(path);
            }

            return result;
        }
    }

    private static string ReadAll(FileStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// One exclusive file transaction: the <see cref="Disk"/> content read under the lock, and a
    /// <see cref="Write"/> that commits new content through the same handle.
    /// </summary>
    internal sealed class Transaction
    {
        private readonly FileStream _stream;

        internal Transaction(FileStream stream, string? disk)
        {
            _stream = stream;
            Disk = disk;
        }

        /// <summary>The file's current content read under the exclusive lock, or <c>null</c> if it did not exist.</summary>
        public string? Disk { get; }

        /// <summary>Whether <see cref="Write"/> committed content in this transaction.</summary>
        internal bool Wrote { get; private set; }

        /// <summary>Replaces the file's content with <paramref name="content"/> through the held handle.</summary>
        public void Write(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            _stream.Position = 0;
            _stream.SetLength(0);
            using var writer = new StreamWriter(_stream, _utf8NoBom, leaveOpen: true);
            writer.Write(content);
            writer.Flush();
            Wrote = true;
        }
    }
}
