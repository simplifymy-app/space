using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Platform.Mac;
using SimplifyMySpace.Platform.Windows;

namespace SimplifyMySpace.App;

public sealed class Services(SettingsStore store)
{
    public Services()
        : this(new SettingsStore())
    {
    }

    public SettingsStore Store { get; } = store;

    public AppSettings Settings => Store.Current;

    public IFileSystem FileSystem { get; } = Pick();

    private static IFileSystem Pick()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsFileSystem();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacFileSystem();
        }

        return new PortableFileSystem();
    }
}
