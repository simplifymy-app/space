using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;
using Shapes = Avalonia.Controls.Shapes;

namespace SimplifyMySpace.App.Theme;

public sealed record Icon(string Path, string Key = "");

public static class Icons
{
    public static readonly Icon Settings =
        new("M3.5 6h7.5M15.5 6h1M3.5 14h1.5M9.5 14h7 M11 6A2.25 2.25 0 1 0 15.5 6A2.25 2.25 0 1 0 11 6Z M5 14A2.25 2.25 0 1 0 9.5 14A2.25 2.25 0 1 0 5 14Z", "icon.settings");

    public static readonly Icon Drive =
        new("M4.25 8H15.75A1.5 1.5 0 0 1 17.25 9.5V13.5A1.5 1.5 0 0 1 15.75 15H4.25A1.5 1.5 0 0 1 2.75 13.5V9.5A1.5 1.5 0 0 1 4.25 8Z M3.2 8.3 5 4.8a1.5 1.5 0 0 1 1.3-.8h7.4a1.5 1.5 0 0 1 1.3.8l1.8 3.5M13.5 11.5h.01");

    public static readonly Icon Usb =
        new("M7 6.5H13A1.5 1.5 0 0 1 14.5 8V15.5A1.5 1.5 0 0 1 13 17H7A1.5 1.5 0 0 1 5.5 15.5V8A1.5 1.5 0 0 1 7 6.5Z M7.5 6.5V3h5v3.5M8.75 4.75h.01M11.25 4.75h.01M10 13.5h.01");

    public static readonly Icon SdCard =
        new("M6.5 2.75h6.25L15.5 5.5v10.25a1.5 1.5 0 0 1-1.5 1.5H6a1.5 1.5 0 0 1-1.5-1.5v-11.5a1.5 1.5 0 0 1 1.5-1.5zM8 5.5v2M10.5 5.5v2M13 6.5v1");

    public static readonly Icon Folder =
        new("M2.75 5.5A1.5 1.5 0 0 1 4.25 4h3.4l1.6 2h6.5a1.5 1.5 0 0 1 1.5 1.5v7a1.5 1.5 0 0 1-1.5 1.5H4.25a1.5 1.5 0 0 1-1.5-1.5z");

    public static readonly Icon FolderPlus =
        new("M2.75 5.5A1.5 1.5 0 0 1 4.25 4h3.4l1.6 2h6.5a1.5 1.5 0 0 1 1.5 1.5v7a1.5 1.5 0 0 1-1.5 1.5H4.25a1.5 1.5 0 0 1-1.5-1.5zM10 8.75v4.5M7.75 11h4.5");

    public static readonly Icon ChevronLeft =
        new("M12.5 4.5 7 10l5.5 5.5");

    public static readonly Icon ChevronRight =
        new("M7.5 4.5 13 10l-5.5 5.5");

    public static readonly Icon Back =
        new("M16 10H4M9 5l-5 5 5 5", "icon.back");

    public static readonly Icon Forward =
        new("M4 10h12M11 5l5 5-5 5", "icon.forward");

    public static readonly Icon Up =
        new("M10 16V4M5.5 8.5L10 4l4.5 4.5", "icon.up");

    public static readonly Icon Lock =
        new("M6 9H14A1.5 1.5 0 0 1 15.5 10.5V15.5A1.5 1.5 0 0 1 14 17H6A1.5 1.5 0 0 1 4.5 15.5V10.5A1.5 1.5 0 0 1 6 9Z M7 9V6.5a3 3 0 0 1 6 0V9");

    public static readonly Icon Stop =
        new("M7.5 6H12.5A1.5 1.5 0 0 1 14 7.5V12.5A1.5 1.5 0 0 1 12.5 14H7.5A1.5 1.5 0 0 1 6 12.5V7.5A1.5 1.5 0 0 1 7.5 6Z");

    public static readonly Icon Check =
        new("M5 10.5 8.2 13.5 15 6.5");

    public static readonly Icon Minus =
        new("M5.5 10h9");

    public static readonly Icon Trash =
        new("M3.5 5.5h13M8 5.5V4a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v1.5M5 5.5l.8 10.1A1.5 1.5 0 0 0 7.3 17h5.4a1.5 1.5 0 0 0 1.5-1.4L15 5.5");

    public static readonly Icon Open =
        new("M11 3.5h5.5V9M16.5 3.5l-7 7M14.5 12v3a1.5 1.5 0 0 1-1.5 1.5H5A1.5 1.5 0 0 1 3.5 15V7A1.5 1.5 0 0 1 5 5.5h3");

    public static readonly Icon Reveal =
        new("M4.5 4H15.5A1.5 1.5 0 0 1 17 5.5V14.5A1.5 1.5 0 0 1 15.5 16H4.5A1.5 1.5 0 0 1 3 14.5V5.5A1.5 1.5 0 0 1 4.5 4Z M3 7.5h14");

    public static readonly Icon Copy =
        new("M8.5 7H15A1.5 1.5 0 0 1 16.5 8.5V15A1.5 1.5 0 0 1 15 16.5H8.5A1.5 1.5 0 0 1 7 15V8.5A1.5 1.5 0 0 1 8.5 7Z M13 7V5a1.5 1.5 0 0 0-1.5-1.5h-6A1.5 1.5 0 0 0 4 5v6a1.5 1.5 0 0 0 1.5 1.5H7");

    public static readonly Icon Close =
        new("M5.5 5.5l9 9M14.5 5.5l-9 9", "icon.unmark");

    public static readonly Icon Info =
        new("M3 10A7 7 0 1 0 17 10A7 7 0 1 0 3 10Z M10 9v4.5M10 6.5h.01");
}

public static class Glyph
{
    public const double IconButton = 32;

    public static Shapes.Path Create(Icon icon, Ink? ink = Ink.Text, double size = Sizes.Icon, double stroke = 1.5)
    {
        var scale = size / Sizes.Icon;
        var path = new Shapes.Path
        {
            Data = Geometry.Parse(icon.Path),
            StrokeThickness = stroke,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Width = Sizes.Icon,
            Height = Sizes.Icon,
            Stretch = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = new ScaleTransform(scale, scale),
            Margin = new Thickness((size - Sizes.Icon) / 2)
        };

        return ink is { } bound ? Ui.Paint(path, Shapes.Shape.StrokeProperty, bound) : path;
    }

    public static Button Button(Icon icon, Action click, string? shortcut = null, Ink ink = Ink.TextMuted)
    {
        var button = new Button
        {
            Width = IconButton,
            Height = IconButton,
            Padding = new Thickness(0),
            Content = Create(icon, ink),
            Classes = { "ghost" }
        };

        AutomationProperties.SetName(button, Strings.English(icon.Key));
        var label = Strings.Get(icon.Key);
        ToolTip.SetTip(button, shortcut is null ? label : $"{label}  {shortcut}");
        button.Click += (_, _) => click();
        return button;
    }

    public static StackPanel Label(Icon icon, Control text, Ink ink = Ink.Text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Space.Base };
        row.Children.Add(Create(icon, ink));
        row.Children.Add(text);
        return row;
    }
}
