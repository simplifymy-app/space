namespace SimplifyMySpace.Core.Files;

public sealed class Protection(IReadOnlyList<string> systemFolders, IReadOnlyList<string> keep)
{
    public static readonly Protection ForThisMachine = new(SystemFolders(), Kept());

    public bool IsProtected(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.GetPathRoot(full) is { } root && ScanRules.Same(root, full)
            || Volumes.IsMountPoint(full)
            || systemFolders.Any(folder => ScanRules.IsWithin(full, folder))
            || keep.Any(kept => ScanRules.IsWithin(kept, full));
    }

    private static string[] SystemFolders()
    {
        if (OperatingSystem.IsWindows())
        {
            return
            [
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            ];
        }

        return ["/System", "/usr", "/bin", "/sbin", "/private", "/etc", "/var", "/dev", "/cores"];
    }

    private static string[] Kept() =>
    [
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        AppContext.BaseDirectory,
        AppData.Folder
    ];
}
