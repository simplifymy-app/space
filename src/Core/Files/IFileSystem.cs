using System.Diagnostics;
using System.IO.Enumeration;

namespace SimplifyMySpace.Core.Files;

public readonly record struct FileIdentity(long Device, ulong Inode);

public enum EntryKind
{
    File,
    Directory,
    Link
}

public readonly record struct DirectoryEntry(
    string Name,
    EntryKind Kind,
    long Logical,
    long OnDisk,
    DateTime Modified,
    bool IsHidden,
    FileIdentity? SharedIdentity);

public interface IFileSystem
{
    bool TryList(string folder, out IReadOnlyList<DirectoryEntry> entries);

    void Trash(string path);

    void Reveal(string path);
}

public class PortableFileSystem : IFileSystem
{
    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = false,
        AttributesToSkip = 0,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false
    };

    public bool TryList(string folder, out IReadOnlyList<DirectoryEntry> entries)
    {
        try
        {
            entries = new FileSystemEnumerable<DirectoryEntry>(folder, (ref entry) => Read(ref entry), Options).ToList();
            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException
                                              or System.Security.SecurityException)
        {
            entries = [];
            return false;
        }
    }

    public virtual void Trash(string path) =>
        throw new PlatformNotSupportedException("Moving to the trash is not supported on this platform");

    public virtual void Reveal(string path) => Open(Path.GetDirectoryName(path) ?? path);

    public static void Open(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();

    public static void Delete(string path)
    {
        if (Directory.Exists(path) && !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            Directory.Delete(path, true);
        }
        else
        {
            File.Delete(path);
        }
    }

    protected virtual long Allocated(ReadOnlySpan<char> folder, long length) => length;

    private DirectoryEntry Read(ref FileSystemEntry entry)
    {
        var kind = entry.Attributes.HasFlag(FileAttributes.ReparsePoint) ? EntryKind.Link
            : entry.IsDirectory ? EntryKind.Directory
            : EntryKind.File;
        var length = kind == EntryKind.File ? entry.Length : 0;
        return new DirectoryEntry(
            entry.FileName.ToString(),
            kind,
            length,
            Allocated(entry.Directory, length),
            entry.LastWriteTimeUtc.LocalDateTime,
            entry.IsHidden,
            null);
    }
}
