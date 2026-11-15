using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class StartView : DockPanel
{
    private const double Padding = 44;

    private readonly MainWindow _main;
    private readonly Services _services;
    private readonly List<Location> _locations = [];
    private readonly TextBlock _summary = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private string? _selected;

    public StartView(MainWindow main, Services services)
    {
        _main = main;
        _services = services;
        var volumes = Volumes.List();
        var last = services.Settings.LastLocation;
        _selected = Directory.Exists(last) ? last : volumes.FirstOrDefault()?.Path;

        var panel = new StackPanel();
        foreach (var volume in volumes)
        {
            panel.Children.Add(Row(volume.Path, VolumeIcon(volume), VolumeName(volume), Kind(volume), volume));
        }

        if (_selected is { } folder && volumes.All(volume => !ScanRules.Same(volume.Path, folder)))
        {
            var folderName = System.IO.Path.GetFileName(folder.TrimEnd(System.IO.Path.DirectorySeparatorChar));
            panel.Children.Add(Row(folder, Icons.Folder, folderName, Format.MiddleTruncate(folder, 60), null));
        }

        for (var index = 1; index < panel.Children.Count; index++)
        {
            ((Border)panel.Children[index]).BorderThickness = new Thickness(0, Sizes.Hairline, 0, 0);
        }

        var volumesPanel = new Border
        {
            CornerRadius = new CornerRadius(Radius.Panel),
            BorderThickness = new Thickness(Sizes.Hairline),
            ClipToBounds = true,
            Child = panel
        };
        Ui.Paint(volumesPanel, Border.BackgroundProperty, Ink.Surface);
        Ui.Paint(volumesPanel, Border.BorderBrushProperty, Ink.Hairline);

        var section = Ui.Section(Strings.Get("start.section"));
        var body = new StackPanel
        {
            Spacing = Space.Inset,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { section, volumesPanel, ChooseRow() }
        };

        var content = new StackPanel
        {
            Margin = new Thickness(Padding, Padding, Padding, Space.Section),
            Spacing = 32,
            Children = { Header(volumes.Count), body }
        };

        var footer = Footer();
        SetDock(footer, Dock.Bottom);
        Children.AddRange([footer, new ScrollViewer { Content = content }]);
        AddHandler(KeyDownEvent, (_, e) =>
        {
            var command = e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control);
            if (e.Key == Key.O && command)
            {
                _ = ChooseFolder();
                e.Handled = true;
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Ui.Follow(this, Repaint);
    }

    public static string VolumeName(Volume volume) =>
        volume.IsStartup ? Strings.Get("start.startup") : volume.Label;

    private static Icon VolumeIcon(Volume volume) => volume.Kind == VolumeKind.External ? Icons.Usb : Icons.Drive;

    private static string Kind(Volume volume) =>
        string.IsNullOrEmpty(volume.Format)
            ? Strings.Caption(volume.Kind)
            : $"{Strings.Caption(volume.Kind)} · {volume.Format}";

    private Control Header(int count)
    {
        var settings = Glyph.Button(Icons.Settings, _main.OpenSettings);
        settings.VerticalAlignment = VerticalAlignment.Center;
        var title = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Ui.Text(Strings.Get("start.title"), Typography.Display),
                Ui.Text(Strings.Get("start.volumes", count), Typography.Meta, Ink.TextMuted)
            }
        };
        var mark = new AppMark(40) { Margin = new Thickness(0, 0, Space.RowGap, 0) };
        var header = new DockPanel();
        SetDock(mark, Dock.Left);
        SetDock(settings, Dock.Right);
        header.Children.AddRange([mark, settings, title]);
        return header;
    }

    private Border Row(string path, Icon icon, string name, string kind, Volume? volume)
    {
        var glyph = Glyph.Create(icon, null);
        var tile = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(Radius.Control),
            Child = glyph
        };

        var titleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.Inset,
            Children = { Ui.Text(name, Typography.Label), Ui.Text(kind, Typography.Meta, Ink.TextMuted) }
        };
        var top = new DockPanel();
        if (volume is not null)
        {
            var used = Ui.Text(Strings.Get("start.used", Bytes(volume.Used), Bytes(volume.Total)), Typography.Meta,
                Ink.TextMuted);
            used.Margin = new Thickness(Space.Inset, 0, 0, 0);
            SetDock(used, Dock.Right);
            top.Children.Add(used);
        }

        top.Children.Add(titleRow);
        var text = new StackPanel { Spacing = Space.Base, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(top);
        var bar = new UsageBar();
        if (volume is not null)
        {
            bar.Show(volume.Total, volume.Used, 0);
            text.Children.Add(bar);
        }

        var free = Ui.Text(volume is null ? string.Empty : Strings.Get("start.free", Bytes(volume.Free)),
            Typography.Value, null);
        free.Width = 120;
        free.TextAlignment = TextAlignment.Right;

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = Space.RowGap };
        Grid.SetColumn(text, 1);
        Grid.SetColumn(free, 2);
        grid.Children.AddRange([tile, text, free]);

        var row = new Border
        {
            Height = 72,
            Padding = new Thickness(Space.RowGap, 0, Space.Screen, 0),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = grid
        };
        Ui.Paint(row, Border.BorderBrushProperty, Ink.Hairline);
        row.PointerPressed += (_, e) =>
        {
            _selected = path;
            Repaint();
            if (e.ClickCount == 2)
            {
                _main.Scan(path);
            }
        };

        _locations.Add(new Location(path, name, volume, row, tile, glyph, bar, free));
        return row;
    }

    private Control ChooseRow()
    {
        var label = Ui.Text(Strings.Get("start.chooseFolder"), Typography.Label);
        var shortcut = Ui.Text(OperatingSystem.IsMacOS() ? "⌘O" : "Ctrl+O", Typography.Meta, Ink.TextFaint);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("40,*,Auto"), ColumnSpacing = Space.RowGap };
        Grid.SetColumn(label, 1);
        Grid.SetColumn(shortcut, 2);
        grid.Children.AddRange([Glyph.Create(Icons.FolderPlus, Ink.TextMuted), label, shortcut]);

        var outline = new Rectangle
        {
            RadiusX = Radius.Panel,
            RadiusY = Radius.Panel,
            StrokeThickness = Sizes.Hairline,
            StrokeDashArray = [4, 3]
        };
        Ui.Paint(outline, Shape.StrokeProperty, Ink.Hairline);
        var row = new Panel
        {
            Height = 56,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Children = { outline, new Border { Padding = new Thickness(Space.RowGap, 0), Child = grid } }
        };
        row.PointerPressed += (_, _) => _ = ChooseFolder();
        return row;
    }

    private Control Footer()
    {
        var scan = Ui.Button("start.scan", () =>
        {
            if (_selected is { } path)
            {
                _main.Scan(path);
            }
        }, "primary");
        scan.Width = 160;
        scan.Height = 44;
        scan.VerticalAlignment = VerticalAlignment.Center;

        var row = new DockPanel { Margin = new Thickness(Padding, 0) };
        SetDock(scan, Dock.Right);
        row.Children.AddRange([scan, _summary]);
        var footer = new Border { Height = 76, BorderThickness = new Thickness(0, Sizes.Hairline, 0, 0), Child = row };
        Ui.Paint(footer, Border.BorderBrushProperty, Ink.Hairline);
        return footer;
    }

    private async Task ChooseFolder()
    {
        var picked = await _main.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (picked.FirstOrDefault()?.TryGetLocalPath() is { } path)
        {
            _main.Scan(path);
        }
    }

    private string Bytes(long bytes) => Format.Bytes(bytes, _services.Settings.Units);

    private void Repaint()
    {
        var colors = AppTheme.Colors;
        foreach (var location in _locations)
        {
            var selected = location.Path == _selected;
            location.Row.Background = selected ? AppTheme.Brush(colors.Accent, States.Selected) : Brushes.Transparent;
            location.Tile.Background = AppTheme.Brush(selected ? colors.Accent : colors.SurfaceAlt);
            location.Glyph.Stroke = AppTheme.Brush(selected ? colors.OnAccent : colors.TextMuted);
            location.Free.Foreground = AppTheme.Brush(selected ? colors.Text : colors.TextMuted);
            location.Bar.Highlight = selected;
        }

        var current = _locations.FirstOrDefault(location => location.Path == _selected);
        _summary.Text = current?.Volume is { } volume
            ? Strings.Get("start.summary", current.Name, Bytes(volume.Used))
            : current?.Name ?? string.Empty;
    }

    private sealed record Location(
        string Path,
        string Name,
        Volume? Volume,
        Border Row,
        Border Tile,
        Avalonia.Controls.Shapes.Path Glyph,
        UsageBar Bar,
        TextBlock Free);
}
