using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Files;

public sealed record RemovalResult(IReadOnlyList<FileNode> Removed, IReadOnlyList<FileNode> Failed, long Freed);

public static class Remover
{
    public static RemovalResult Run(
        IReadOnlyList<FileNode> items,
        Action<string> remove,
        SizeKind kind,
        IProgress<int> progress,
        CancellationToken token)
    {
        var removed = new List<FileNode>();
        var failed = new List<FileNode>();
        foreach (var item in items)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                remove(item.Path);
                removed.Add(item);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                                  or PlatformNotSupportedException)
            {
                Log.Error($"Could not remove {item.Path}", exception);
                failed.Add(item);
            }

            progress.Report(removed.Count + failed.Count);
        }

        return new RemovalResult(removed, failed, removed.Sum(item => item.Size(kind)));
    }
}
