using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class ExploreView : Grid
{
    private readonly MainWindow _main;
    private readonly Explore _explore;
    private readonly TopBar _top;
    private readonly ScanBanner _banner;
    private readonly ScanScreen _loading;
    private readonly NodeList _list;
    private readonly MapView _map;
    private readonly BottomBar _bottom;
    private readonly TextBlock _title = Ui.Section(string.Empty);
    private readonly TextBlock _meta = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(400) };

    public ExploreView(MainWindow main, Explore explore)
    {
        _main = main;
        _explore = explore;
        var session = explore.Session;
        _top = new TopBar(session.Navigator, StartOver, main.OpenSettings);
        _banner = new ScanBanner(explore);
        _loading = new ScanScreen(explore);
        _list = new NodeList(explore);
        _map = new MapView(explore) { Margin = new Thickness(Space.Screen) };
        _bottom = new BottomBar(explore, () => _ = Review());

        RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto");
        var headerRow = new DockPanel { Margin = new Thickness(Space.RowGap, 0, Space.Inset, 0) };
        DockPanel.SetDock(_meta, Dock.Right);
        headerRow.Children.AddRange([_meta, _title]);
        var header = new Border
        {
            Height = 40,
            BorderThickness = new Thickness(0, 0, 0, Sizes.Hairline),
            Child = headerRow
        };
        Ui.Paint(header, Border.BorderBrushProperty, Ink.Hairline);
        var left = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        left.Children.AddRange([header, _list]);

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("400,Auto,*"),
            Children = { left, new GridSplitter { ResizeDirection = GridResizeDirection.Columns }, _map }
        };
        content.ColumnDefinitions[0].MinWidth = 320;
        SetColumn(content.Children[1], 1);
        SetColumn(_map, 2);

        var topBorder = new Border { Child = _top, BorderThickness = new Thickness(0, 0, 0, Sizes.Hairline) };
        Ui.Paint(topBorder, Border.BorderBrushProperty, Ink.Hairline);
        SetRow(_banner, 1);
        SetRow(content, 2);
        SetRow(_bottom, 3);
        SetRow(_loading, 1);
        SetRowSpan(_loading, 3);
        Children.AddRange([topBorder, _banner, content, _bottom]);
        if (explore.IsScanning)
        {
            Children.Add(_loading);
        }

        _loading.IsVisible = Loading;

        _ticker.Tick += (_, _) => Tick();
        AddHandler(KeyDownEvent, Keys, RoutingStrategies.Tunnel);
        AttachedToVisualTree += (_, _) => Subscribe(true);
        DetachedFromVisualTree += (_, _) => Subscribe(false);
        Navigated();
    }

    private void Subscribe(bool on)
    {
        var session = _explore.Session;
        if (on)
        {
            session.Navigator.Changed += Navigated;
            session.Basket.Changed += Marked;
            session.SelectionChanged += Selected;
            session.HoverChanged += Hovered;
            _explore.Services.Store.Changed += Refresh;
            _ticker.Start();
            _list.FocusList();
        }
        else
        {
            session.Navigator.Changed -= Navigated;
            session.Basket.Changed -= Marked;
            session.SelectionChanged -= Selected;
            session.HoverChanged -= Hovered;
            _explore.Services.Store.Changed -= Refresh;
            _ticker.Stop();
        }
    }

    private void Tick()
    {
        var finished = !_explore.IsScanning;
        Refresh();
        if (finished)
        {
            _ticker.Stop();
            if (_explore.WasStopped)
            {
                _main.Toast(Strings.Get("scan.stopped"));
            }
        }
    }

    private bool Loading => _explore.IsScanning && _explore.Settings.ScanAnimation;

    private void Refresh()
    {
        _loading.IsVisible = Loading;
        if (Loading)
        {
            _loading.Update();
            return;
        }

        if (!_explore.IsScanning)
        {
            Children.Remove(_loading);
        }

        _list.Refresh();
        _map.Relayout();
        _banner.Update();
        _bottom.Update();
        Header();
    }

    private void Navigated()
    {
        var folder = _explore.Session.Navigator.Current;
        _top.Update();
        _list.Show(folder);
        _map.Show(folder);
        Header();
    }

    private void Header()
    {
        var folder = _explore.Session.Navigator.Current;
        var scanner = _explore.Session.Scanner;
        var name = folder.Parent is null ? folder.Path : folder.Name;
        _title.Text = Strings.Get("explore.header", name, Format.Count(folder.Children.Count)).ToUpperInvariant();
        _meta.Text = scanner.Unreadable > 0 && !_explore.IsScanning
            ? Strings.Get("explore.unreadable", Format.Count(scanner.Unreadable))
            : string.Empty;
    }

    private void Marked()
    {
        _list.Repaint();
        _map.InvalidateVisual();
        _bottom.Update();
    }

    private void Selected()
    {
        _list.Reveal(_explore.Session.Selected);
        _map.InvalidateVisual();
    }

    private void Hovered()
    {
        _list.Repaint();
        _map.InvalidateVisual();
    }

    private void StartOver()
    {
        _explore.Stop();
        _main.ShowStart();
    }

    private void Keys(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox)
        {
            return;
        }

        var session = _explore.Session;
        var command = e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control);
        e.Handled = (e.Key, command) switch
        {
            (Key.Back, true) => Run(() => _ = Review()),
            (Key.Back, false) => Run(session.Navigator.Up),
            (Key.OemOpenBrackets, true) => Run(session.Navigator.Back),
            (Key.OemCloseBrackets, true) => Run(session.Navigator.Forward),
            (Key.Enter, false) when session.Selected is { } node => Run(() => _explore.Activate(node)),
            (Key.Space, false) when session.Selected is { } node => Run(() => _explore.ToggleMark(node)),
            _ => false
        };
    }

    private static bool Run(Action action)
    {
        action();
        return true;
    }

    private async Task Review()
    {
        var basket = _explore.Session.Basket;
        if (basket.Items.Count == 0 || _explore.IsScanning)
        {
            return;
        }

        var mode = await new ReviewWindow(_explore).ShowDialog<RemoveMode?>(_main);
        if (mode is not { } chosen || basket.Items.Count == 0)
        {
            return;
        }

        var items = basket.Items.ToList();
        Action<string> remove = chosen == RemoveMode.Trash ? _explore.Services.FileSystem.Trash : PortableFileSystem.Delete;
        var progress = new Progress<int>(done =>
            _main.Toast(Strings.Get("remove.progress", Format.Count(done), Format.Count(items.Count)), false));
        var kind = _explore.Settings.SizeKind;
        var result = await Task.Run(() => Remover.Run(items, remove, kind, progress, CancellationToken.None));

        _explore.Session.Forget(result.Removed);
        Navigated();
        Refresh();
        var done = chosen == RemoveMode.Trash
            ? Strings.Get(OperatingSystem.IsWindows() ? "remove.trashed.windows" : "remove.trashed.mac",
                _explore.Bytes(result.Freed))
            : Strings.Get("remove.deleted", _explore.Bytes(result.Freed));
        _main.Toast(result.Failed.Count == 0
            ? done
            : $"{done} · {Strings.Get("remove.failed", Format.Count(result.Failed.Count))}");
    }
}
