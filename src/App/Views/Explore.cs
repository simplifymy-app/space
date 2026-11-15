using System.Diagnostics;
using Avalonia.Controls;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Text;

namespace SimplifyMySpace.App.Views;

public sealed class Explore(Session session, Services services, Task scan, CancellationTokenSource stop)
{
    public Session Session => session;

    public Services Services => services;

    public AppSettings Settings => services.Settings;

    public Task Scan => scan;

    public bool IsScanning => !scan.IsCompleted;

    public bool WasStopped => stop.IsCancellationRequested;

    public Stopwatch Clock { get; } = Stopwatch.StartNew();

    public Volume? Volume { get; } = Volumes.Containing(session.Root.Path);

    public bool IsVolume => Volume is not null && ScanRules.Same(Volume.Path, session.Root.Path);

    public long Counted => session.Root.Size(services.Settings.SizeKind);

    public double Share => IsVolume && Volume!.Used > 0 ? Math.Min(1, (double)Counted / Volume.Used) : 0;

    public string RootName => IsVolume
        ? StartView.VolumeName(Volume!)
        : Path.GetFileName(session.Root.Path.TrimEnd(Path.DirectorySeparatorChar));

    public void Stop() => stop.Cancel();

    public bool CanMark(FileNode node) => !IsScanning && session.Basket.CanMark(node);

    public void ToggleMark(FileNode node)
    {
        if (!IsScanning)
        {
            session.Basket.Toggle(node);
        }
    }

    public string Bytes(long bytes) => Format.Bytes(bytes, Settings.Units);

    public string Size(FileNode node) => Bytes(node.Size(Settings.SizeKind));

    public string Describe(FileNode node)
    {
        if (!node.IsDirectory)
        {
            return Format.Date(node.Modified);
        }

        return KnownFolders.Key(node.Path) is { } key
            ? Strings.Get(key)
            : Strings.Get("explore.meta", Format.Count(node.Items), Format.Date(node.Modified));
    }

    public void Activate(FileNode node)
    {
        if (node.IsDirectory)
        {
            session.Navigator.Open(node);
        }
        else
        {
            session.Select(node);
        }
    }

    public ContextMenu Menu(Control owner, FileNode node)
    {
        var basket = session.Basket;
        var items = new List<MenuItem>
        {
            Item(Icons.Reveal, Strings.ForPlatform("menu.reveal"), () => services.FileSystem.Reveal(node.Path)),
            Item(Icons.Open, Strings.Get("menu.open"), () => PortableFileSystem.Open(node.Path)),
            Item(Icons.Copy, Strings.Get("menu.copyPath"),
                () => TopLevel.GetTopLevel(owner)?.Clipboard?.SetTextAsync(node.Path))
        };

        if (node.IsDirectory)
        {
            items.Insert(0, Item(Icons.ChevronRight, Strings.Get("menu.enter"), () => session.Navigator.Open(node)));
        }

        if (CanMark(node) && !basket.IsCovered(node) || basket.IsMarked(node))
        {
            var marked = basket.IsMarked(node);
            items.Add(Item(marked ? Icons.Minus : Icons.Check, Strings.Get(marked ? "menu.unmark" : "menu.mark"),
                () => ToggleMark(node)));
        }

        return new ContextMenu { ItemsSource = items };
    }

    private static MenuItem Item(Icon icon, string header, Action action)
    {
        var item = new MenuItem { Header = header, Icon = Glyph.Create(icon, Ink.TextMuted, 18) };
        item.Click += (_, _) => action();
        return item;
    }
}
