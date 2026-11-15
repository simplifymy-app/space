namespace SimplifyMySpace.Core.Files;

public enum VolumeKind
{
    Internal,
    External,
    Network
}

public sealed record Volume(string Label, string Path, long Total, long Free, VolumeKind Kind, string Format)
{
    public long Used => Total - Free;

    public bool IsStartup => Path == "/";
}

public static class Volumes
{
    public static IReadOnlyList<Volume> List()
    {
        var volumes = Ready().Where(Listed).Select(ToVolume).ToList();
        var startup = volumes.FirstOrDefault(volume => volume.IsStartup);
        return volumes
            .Where(volume => volume == startup || startup is null
                || volume.Total != startup.Total || volume.Free != startup.Free)
            .ToList();
    }

    public static Volume? Containing(string path) =>
        Ready()
            .Where(drive => ScanRules.IsWithin(path, drive.RootDirectory.FullName))
            .OrderByDescending(drive => drive.RootDirectory.FullName.Length)
            .Select(ToVolume)
            .FirstOrDefault();

    public static bool IsMountPoint(string path) =>
        !OperatingSystem.IsWindows() && Ready().Any(drive => ScanRules.Same(drive.RootDirectory.FullName, path));

    private static IEnumerable<DriveInfo> Ready() =>
        DriveInfo.GetDrives().Where(drive =>
        {
            try
            {
                return drive.IsReady && drive.TotalSize > 0;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        });

    private static bool Listed(DriveInfo drive)
    {
        if (OperatingSystem.IsWindows())
        {
            return drive.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Network;
        }

        var root = drive.RootDirectory.FullName;
        return root == "/" || root.StartsWith("/Volumes/", StringComparison.Ordinal)
            || root.StartsWith("/media/", StringComparison.Ordinal)
            || root.StartsWith("/mnt/", StringComparison.Ordinal);
    }

    private static Volume ToVolume(DriveInfo drive)
    {
        var root = drive.RootDirectory.FullName;
        var label = OperatingSystem.IsWindows()
            ? $"{drive.VolumeLabel} ({root.TrimEnd('\\')})".TrimStart()
            : root == "/" ? string.Empty : System.IO.Path.GetFileName(root.TrimEnd('/'));
        var kind = drive.DriveType switch
        {
            DriveType.Network => VolumeKind.Network,
            DriveType.Removable => VolumeKind.External,
            _ when !OperatingSystem.IsWindows() && root != "/" => VolumeKind.External,
            _ => VolumeKind.Internal
        };
        return new Volume(label, root, drive.TotalSize, drive.AvailableFreeSpace, kind, FormatName(drive));
    }

    private static string FormatName(DriveInfo drive)
    {
        var format = drive.DriveFormat;
        return format.ToLowerInvariant() switch
        {
            "apfs" => "APFS",
            "hfs" => "HFS+",
            "exfat" => "exFAT",
            "msdos" or "fat32" => "FAT32",
            "ntfs" => "NTFS",
            "smbfs" => "SMB",
            _ => format
        };
    }
}
