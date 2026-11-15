namespace SimplifyMySpace.Core.Tests;

public sealed class TempTree : IDisposable
{
    public TempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), "space-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string File(string relative, int bytes)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllBytes(path, new byte[bytes]);
        return path;
    }

    public string Folder(string relative) => Directory.CreateDirectory(Path.Combine(Root, relative)).FullName;

    public void Dispose()
    {
        foreach (var folder in Directory.EnumerateDirectories(Root, "*", SearchOption.AllDirectories))
        {
            if (OperatingSystem.IsWindows())
            {
                break;
            }

            System.IO.File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Directory.Delete(Root, true);
    }
}
