using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.VisualBasic.FileIO;
using SimplifyMySpace.Core.Files;

namespace SimplifyMySpace.Platform.Windows;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsFileSystem : PortableFileSystem
{
    private readonly ConcurrentDictionary<string, long> _clusters = new(StringComparer.OrdinalIgnoreCase);

    public override void Trash(string path)
    {
        if (Directory.Exists(path))
        {
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
        else
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
    }

    public override void Reveal(string path) => Process.Start("explorer.exe", $"/select,\"{path}\"")?.Dispose();

    protected override long Allocated(ReadOnlySpan<char> folder, long length)
    {
        var cluster = Cluster(Path.GetPathRoot(folder).ToString());
        return cluster > 0 ? (length + cluster - 1) / cluster * cluster : length;
    }

    private long Cluster(string root) => _clusters.GetOrAdd(root, key =>
        GetDiskFreeSpaceW(key, out var sectorsPerCluster, out var bytesPerSector, out _, out _)
            ? (long)sectorsPerCluster * bytesPerSector
            : 0);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetDiskFreeSpaceW(string root, out uint sectorsPerCluster, out uint bytesPerSector,
        out uint freeClusters, out uint totalClusters);
}
