namespace SimplifyMySpace.Core.Files;

public static class KnownFolders
{
    private static readonly Lazy<(string Path, string Key)[]> Table = new(Build);

    public static string? Key(string path) =>
        Table.Value.FirstOrDefault(entry => ScanRules.Same(entry.Path, path)).Key;

    private static (string, string)[] Build()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsWindows())
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return
            [
                (windows, "known.windows"),
                (Path.Combine(windows, "WinSxS"), "known.winsxs"),
                (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "known.programFiles"),
                (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "known.programFiles"),
                (Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "known.programData"),
                (Path.GetDirectoryName(home) ?? home, "known.users"),
                (Path.Combine(home, "AppData"), "known.appData")
            ];
        }

        return
        [
            ("/Users", "known.users"),
            ("/home", "known.users"),
            ("/Applications", "known.applications"),
            ("/System", "known.system"),
            ("/Library", "known.library"),
            ("/private", "known.private"),
            ("/usr", "known.usr"),
            ("/opt", "known.opt"),
            ("/bin", "known.bin"),
            ("/sbin", "known.bin"),
            (Path.Combine(home, "Library"), "known.userLibrary"),
            (Path.Combine(home, "Library", "Caches"), "known.caches"),
            (Path.Combine(home, ".cache"), "known.caches")
        ];
    }
}
