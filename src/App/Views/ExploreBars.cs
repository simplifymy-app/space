using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class TopBar : DockPanel
{
    private readonly Navigator _navigator;
    private readonly Button _back;
    private readonly Button _forward;
    private readonly Button _up;
    private readonly StackPanel _crumbs = new() { Orientation = Orientation.Horizontal, Spacing = 2 };

    public TopBar(Navigator navigator, Action startOver, Action settings)
    {
        _navigator = navigator;
        Height = Sizes.TopBar;
        Margin = new Thickness(Space.Inset, 0);

        var restartLabel = Ui.Text(Strings.Get("explore.startOver"), Typography.Label, Ink.TextMuted);
        restartLabel.FontSize = 13;
        var restart = new Button
        {
            Classes = { "ghost" },
            Height = Glyph.IconButton,
            Padding = new Thickness(4, 0, 10, 0),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children = { Glyph.Create(Icons.ChevronLeft, Ink.TextMuted), restartLabel }
            }
        };
        restart.Click += (_, _) => startOver();

        var command = OperatingSystem.IsMacOS() ? "⌘" : "Ctrl+";
        _back = Glyph.Button(Icons.Back, navigator.Back, command + "[");
        _forward = Glyph.Button(Icons.Forward, navigator.Forward, command + "]");
        _up = Glyph.Button(Icons.Up, navigator.Up, "⌫");

        var divider = Ui.Hairline(true);
        divider.Height = 20;
        divider.Margin = new Thickness(6, 0);
        var leading = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { restart, divider, _back, _forward, _up }
        };

        var trailing = Glyph.Button(Icons.Settings, settings);
        trailing.VerticalAlignment = VerticalAlignment.Center;
        SetDock(leading, Dock.Left);
        SetDock(trailing, Dock.Right);
        _crumbs.VerticalAlignment = VerticalAlignment.Center;
        _crumbs.Margin = new Thickness(Space.Base, 0);
        Children.AddRange([leading, trailing, _crumbs]);
        Update();
    }

    public void Update()
    {
        _back.IsEnabled = _navigator.CanGoBack;
        _forward.IsEnabled = _navigator.CanGoForward;
        _up.IsEnabled = _navigator.CanGoUp;
        _crumbs.Children.Clear();

        var chain = _navigator.Current.Ancestry().SkipWhile(node => node != _navigator.Root).ToList();
        var shown = chain.Count > 5 ? [chain[0], null, .. chain.TakeLast(3)] : chain.Cast<FileNode?>().ToList();
        foreach (var node in shown)
        {
            if (_crumbs.Children.Count > 0)
            {
                _crumbs.Children.Add(Ui.Text("/", Typography.Value, Ink.TextFaint));
            }

            _crumbs.Children.Add(node is null ? Ui.Text("…", Typography.Value, Ink.TextFaint) : Crumb(node));
        }
    }

    private Control Crumb(FileNode node)
    {
        var current = node == _navigator.Current;
        var label = Ui.Text(Format.MiddleTruncate(node.Name, 40), Typography.Value, current ? Ink.Text : Ink.TextMuted);
        var button = new Button
        {
            Classes = { "ghost" },
            Height = 28,
            MinWidth = 0,
            Padding = new Thickness(6, 0),
            Content = label
        };
        button.Click += (_, _) => _navigator.Open(node);
        return button;
    }
}

public sealed class ScanBanner : Border
{
    private readonly Explore _explore;
    private readonly TextBlock _path = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly TextBlock _size = Ui.Text(string.Empty, Typography.Value);
    private readonly TextBlock _items = Ui.Text(string.Empty, Typography.Value, Ink.TextMuted);
    private readonly TextBlock _clock = Ui.Text(string.Empty, Typography.Value, Ink.TextMuted);
    private readonly TextBlock _percent = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly TextBlock _unreadable = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly StackPanel _unreadableRow;
    private readonly Border _line = Ui.Surface(Ink.Accent);
    private readonly Border _track = Ui.Surface(Ink.SurfaceAlt);

    public ScanBanner(Explore explore)
    {
        _explore = explore;
        BorderThickness = new Thickness(0, 0, 0, Sizes.Hairline);
        Ui.Paint(this, BackgroundProperty, Ink.Surface);
        Ui.Paint(this, BorderBrushProperty, Ink.Hairline);

        var stop = new Button
        {
            Height = 36,
            Classes = { "secondary" },
            Content = Glyph.Label(Icons.Stop, Ui.Text(Strings.Get("scan.stop"), Typography.Label))
        };
        stop.Click += (_, _) => explore.Stop();

        _unreadableRow = Glyph.Label(Icons.Lock, _unreadable, Ink.TextMuted);
        _unreadableRow.VerticalAlignment = VerticalAlignment.Center;

        var counters = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Space.RowGap };
        counters.Children.AddRange([_size, _items, _clock, _percent]);
        var text = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                Ui.Section(Strings.Get("scan.section", explore.RootName)),
                Ui.Text(Strings.Get("scan.title"), Typography.Headline),
                _path,
                counters
            }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.RowGap,
            VerticalAlignment = VerticalAlignment.Bottom,
            Children = { _unreadableRow, stop }
        };

        var row = new DockPanel { Margin = new Thickness(Space.Screen, Space.Section, Space.Screen, Space.Screen) };
        DockPanel.SetDock(actions, Dock.Right);
        row.Children.AddRange([actions, text]);

        _track.Height = 2;
        _track.VerticalAlignment = VerticalAlignment.Top;
        _line.Height = 2;
        _line.HorizontalAlignment = HorizontalAlignment.Left;
        _line.VerticalAlignment = VerticalAlignment.Top;
        Child = new Panel { Children = { row, _track, _line } };
        Update();
    }

    public void Update()
    {
        IsVisible = _explore.IsScanning && !_explore.Settings.ScanAnimation;
        var share = _explore.Share;
        _path.Text = Format.MiddleTruncate(_explore.Session.Scanner.Current, 110);
        _size.Text = _explore.Bytes(_explore.Counted);
        _items.Text = Strings.Get("explore.items", Format.Count(_explore.Session.Root.Items));
        _clock.Text = Format.Elapsed(_explore.Clock.Elapsed);
        _percent.Text = _explore.IsVolume ? Format.Percent(share) : string.Empty;
        var unreadable = _explore.Session.Scanner.Unreadable;
        _unreadable.Text = Strings.Get("explore.unreadable", Format.Count(unreadable));
        _unreadableRow.IsVisible = unreadable > 0;
        _track.IsVisible = _explore.IsVolume;
        _line.Width = Bounds.Width * share;
    }
}

public sealed class BottomBar : Border
{
    private readonly Explore _explore;
    private readonly TextBlock _volume = Ui.Text(string.Empty, Typography.Label);
    private readonly TextBlock _usage = Ui.Text(string.Empty, Typography.Value, Ink.TextMuted);
    private readonly UsageBar _bar = new() { Width = 140, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _after = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly TextBlock _selection = Ui.Text(string.Empty, Typography.Value);
    private readonly Button _review;
    private readonly Button _remove;

    public BottomBar(Explore explore, Action review)
    {
        _explore = explore;
        Height = Sizes.BottomBar;
        BorderThickness = new Thickness(0, Sizes.Hairline, 0, 0);
        Ui.Paint(this, BackgroundProperty, Ink.Background);
        Ui.Paint(this, BorderBrushProperty, Ink.Hairline);
        _volume.FontSize = 13;

        _review = Ui.Button("bottom.review", review, "secondary");
        _review.Height = 36;
        _remove = new Button
        {
            Height = 36,
            Classes = { "danger" },
            Content = Glyph.Label(Icons.Trash, Ui.Text(Strings.Get("bottom.remove"), Typography.Label, Ink.Danger),
                Ink.Danger)
        };
        _remove.Click += (_, _) => review();

        var usage = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            ClipToBounds = true,
            Margin = new Thickness(0, 0, Space.RowGap, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { Glyph.Create(Icons.Drive, Ink.TextMuted), _volume, _usage, _bar, _after }
        };

        _selection.Margin = new Thickness(0, 0, 4, 0);
        var tray = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.Base,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _selection, _review, _remove }
        };

        var row = new DockPanel { Margin = new Thickness(Space.Screen, 0, Space.Inset, 0) };
        DockPanel.SetDock(tray, Dock.Right);
        row.Children.AddRange([tray, usage]);
        Child = row;
        Update();
    }

    public void Update()
    {
        var basket = _explore.Session.Basket;
        var kind = _explore.Settings.SizeKind;
        var marked = basket.Total(kind);
        var volume = Volumes.Containing(_explore.Session.Root.Path);
        _volume.Text = volume is null ? string.Empty : StartView.VolumeName(volume);
        var scanning = _explore.IsScanning;
        _review.IsVisible = !scanning;
        _remove.IsVisible = !scanning;
        if (scanning)
        {
            var counted = _explore.Session.Root.Size(kind);
            _usage.Text = volume is null
                ? _explore.Bytes(counted)
                : Strings.Get("bottom.counted", _explore.Bytes(counted), _explore.Bytes(volume.Used));
            _bar.Show(volume?.Used ?? 0, counted, 0);
            _after.Text = string.Empty;
            _selection.Text = Strings.Get("bottom.locked");
            Ui.Paint(_selection, TextBlock.ForegroundProperty, Ink.TextFaint);
            return;
        }

        _usage.Text = volume is null
            ? string.Empty
            : Strings.Get("bottom.usage", _explore.Bytes(volume.Used), _explore.Bytes(volume.Total));
        _bar.Show(volume?.Total ?? 0, volume?.Used ?? 0, marked);
        _after.Text = volume is null || marked == 0
            ? string.Empty
            : Strings.Get("bottom.after", _explore.Bytes(marked), _explore.Bytes(volume.Free + marked));
        var any = basket.Items.Count > 0;
        _selection.Text = any
            ? Strings.Get("bottom.selection", Format.Count(basket.Items.Count), _explore.Bytes(marked))
            : Strings.Get("bottom.nothing");
        Ui.Paint(_selection, TextBlock.ForegroundProperty, any ? Ink.Text : Ink.TextFaint);
        _review.IsEnabled = any;
        _remove.IsEnabled = any;
    }
}
