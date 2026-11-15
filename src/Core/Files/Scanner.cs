using System.Collections.Concurrent;

namespace SimplifyMySpace.Core.Files;

public sealed class Scanner
{
    private static readonly int Workers = Math.Clamp(Environment.ProcessorCount * 3, 8, 32);

    private readonly IFileSystem _fileSystem;
    private readonly bool _followLinks;
    private readonly ConcurrentDictionary<FileIdentity, byte> _sharedFiles = new();
    private readonly ConcurrentDictionary<string, byte> _linkedFolders = new(StringComparer.Ordinal);
    private volatile string _current = string.Empty;
    private int _unreadable;

    public Scanner(string path, IFileSystem fileSystem, bool followLinks)
    {
        var full = Path.GetFullPath(path);
        Root = new FileNode(full, null, true, Directory.GetLastWriteTime(full), false);
        _fileSystem = fileSystem;
        _followLinks = followLinks;
    }

    public FileNode Root { get; }

    public string Current => _current;

    public int Unreadable => _unreadable;

    public Task Run(CancellationToken token) =>
        Task.Factory.StartNew(() => Work(token), CancellationToken.None, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private void Work(CancellationToken token)
    {
        var queue = new ConcurrentQueue<(FileNode Folder, string Path)>();
        queue.Enqueue((Root, Root.Path));
        using var ready = new SemaphoreSlim(1);
        using var stop = token.Register(() => ready.Release(Workers));
        var pending = 1;

        void Drain()
        {
            while (true)
            {
                ready.Wait(CancellationToken.None);
                if (token.IsCancellationRequested || !queue.TryDequeue(out var next))
                {
                    return;
                }

                foreach (var child in Visit(next.Folder, next.Path))
                {
                    Interlocked.Increment(ref pending);
                    queue.Enqueue(child);
                    ready.Release();
                }

                if (Interlocked.Decrement(ref pending) == 0)
                {
                    ready.Release(Workers);
                }
            }
        }

        var workers = Enumerable.Range(0, Workers)
            .Select(_ => Task.Factory.StartNew(Drain, CancellationToken.None, TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();
        Task.WaitAll(workers, CancellationToken.None);
        _current = string.Empty;
    }

    private List<(FileNode, string)> Visit(FileNode folder, string path)
    {
        _current = path;
        if (!_fileSystem.TryList(path, out var entries))
        {
            folder.Unreadable = true;
            Interlocked.Increment(ref _unreadable);
            return [];
        }

        var children = new List<FileNode>(entries.Count);
        var folders = new List<(FileNode, string)>();
        long onDisk = 0;
        long logical = 0;
        foreach (var entry in entries)
        {
            if (entry.Kind == EntryKind.File)
            {
                var counted = entry.SharedIdentity is not { } identity || _sharedFiles.TryAdd(identity, 0);
                var file = new FileNode(entry.Name, folder, false, entry.Modified, entry.IsHidden);
                if (counted)
                {
                    file.Measure(entry.OnDisk, entry.Logical);
                    onDisk += entry.OnDisk;
                    logical += entry.Logical;
                }

                children.Add(file);
                continue;
            }

            var childPath = Path.Combine(path, entry.Name);
            if (ScanRules.Skips(childPath) || entry.Kind == EntryKind.Link && !Follows(childPath))
            {
                continue;
            }

            var child = new FileNode(entry.Name, folder, true, entry.Modified, entry.IsHidden);
            children.Add(child);
            folders.Add((child, childPath));
        }

        folder.Adopt(children.ToArray());
        folder.Grow(onDisk, logical, children.Count);
        return folders;
    }

    private bool Follows(string link)
    {
        if (!_followLinks)
        {
            return false;
        }

        try
        {
            var target = new DirectoryInfo(link).ResolveLinkTarget(true)?.FullName;
            return target is not null
                && Directory.Exists(target)
                && !ScanRules.IsWithin(target, Root.Path)
                && _linkedFolders.TryAdd(target, 0);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
