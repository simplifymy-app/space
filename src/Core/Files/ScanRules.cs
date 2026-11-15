namespace SimplifyMySpace.Core.Files;

public static class ScanRules
{
    private static readonly string[] MacSkipped =
    [
        "/System/Volumes",
        "/Volumes",
        "/dev",
        "/net",
        "/home"
    ];

    public static StringComparison Comparison => OperatingSystem.IsLinux()
        ? StringComparison.Ordinal
        : StringComparison.OrdinalIgnoreCase;

    public static bool Skips(string path) =>
        OperatingSystem.IsMacOS() && MacSkipped.Any(skipped => Same(path, skipped));

    public static bool Same(string a, string b) =>
        string.Equals(Trim(a), Trim(b), Comparison);

    public static bool IsWithin(string path, string folder)
    {
        var trimmedFolder = Trim(folder);
        var trimmedPath = Trim(path);
        if (trimmedPath.Length <= trimmedFolder.Length)
        {
            return string.Equals(trimmedPath, trimmedFolder, Comparison);
        }

        return trimmedPath.StartsWith(trimmedFolder, Comparison)
            && (trimmedFolder.EndsWith(Path.DirectorySeparatorChar)
                || trimmedPath[trimmedFolder.Length] == Path.DirectorySeparatorChar);
    }

    private static string Trim(string path) =>
        path.Length > 1 && path != Path.GetPathRoot(path) ? path.TrimEnd(Path.DirectorySeparatorChar) : path;
}
