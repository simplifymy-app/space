using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;
using Shapes = Avalonia.Controls.Shapes;

namespace SimplifyMySpace.App.Views;

public sealed class NodeRow : Grid
{
    private const double ShareWidth = 56;
    private const double Box = 16;

    private readonly Explore _explore;
    private readonly Border _hit;
    private readonly Border _box;
    private readonly Shapes.Path _check = Glyph.Create(Icons.Check, Ink.OnAccent, 14, 2);
    private readonly Shapes.Path _mixed = Glyph.Create(Icons.Minus, Ink.OnAccent, 14, 2);
    private readonly Border _tile;
    private readonly Shapes.Path _folderGlyph = Glyph.Create(Icons.Folder, null);
    private readonly TextBlock _extension = Ui.Text(string.Empty, Typography.Section, null);
    private readonly TextBlock _name = Ui.Text(string.Empty, Typography.Label);
    private readonly TextBlock _meta = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly TextBlock _size = Ui.Text(string.Empty, Typography.Value);
    private readonly Border _share = new() { Height = 2, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly Shapes.Path _chevron = Glyph.Create(Icons.ChevronRight, Ink.TextFaint);

    public NodeRow(Explore explore, NodeList owner)
    {
        _explore = explore;
        Height = Space.Row;
        Background = Brushes.Transparent;
        ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto");
        ColumnSpacing = 10;

        _box = new Border
        {
            Width = Box,
            Height = Box,
            CornerRadius = new CornerRadius(Radius.Control),
            BorderThickness = new Thickness(1.5),
            Child = new Panel { Children = { _check, _mixed } }
        };
        _hit = new Border
        {
            Width = Glyph.IconButton,
            Height = Glyph.IconButton,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = _box
        };
        _hit.PointerPressed += (_, e) =>
        {
            if (Node is { } node)
            {
                explore.ToggleMark(node);
            }

            e.Handled = true;
        };

        _extension.FontSize = 9.5;
        _extension.HorizontalAlignment = HorizontalAlignment.Center;
        _tile = new Border
        {
            Width = Sizes.Tile,
            Height = Sizes.Tile,
            CornerRadius = new CornerRadius(Radius.Control),
            Child = new Panel { Children = { _folderGlyph, _extension } }
        };

        var text = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _name, _meta }
        };

        var track = Ui.Surface(Ink.Hairline, _share);
        track.Width = ShareWidth;
        track.Height = 2;
        track.HorizontalAlignment = HorizontalAlignment.Right;
        _size.HorizontalAlignment = HorizontalAlignment.Right;
        var size = new StackPanel
        {
            Spacing = 6,
            MinWidth = 72,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _size, track }
        };

        _chevron.Cursor = new Cursor(StandardCursorType.Hand);
        _chevron.PointerPressed += (_, e) =>
        {
            if (Node is { } node)
            {
                explore.Activate(node);
            }

            e.Handled = true;
        };

        SetColumn(_tile, 1);
        SetColumn(text, 2);
        SetColumn(size, 3);
        SetColumn(_chevron, 4);
        Children.AddRange([_hit, _tile, text, size, _chevron]);

        PointerEntered += (_, _) => explore.Session.Hover(Node);
        PointerExited += (_, _) => explore.Session.Hover(null);
        ContextRequested += (_, e) =>
        {
            if (Node is { } node)
            {
                explore.Menu(this, node).Open(this);
                e.Handled = true;
            }
        };

        AttachedToVisualTree += (_, _) => owner.Track(this, true);
        DetachedFromVisualTree += (_, _) => owner.Track(this, false);
        DataContextChanged += (_, _) => Update();
        Ui.Follow(this, Paint);
    }

    private FileNode? Node => DataContext as FileNode;

    public void Update()
    {
        if (Node is not { } node)
        {
            return;
        }

        _name.Text = node.Name;
        _size.Text = _explore.Size(node);
        _folderGlyph.IsVisible = node.IsDirectory;
        _extension.IsVisible = !node.IsDirectory;
        _extension.Text = node.Extension.Length is > 0 and <= 4 ? node.Extension.ToUpperInvariant() : "FILE";
        _chevron.Opacity = node.IsDirectory ? 1 : 0;
        var kind = _explore.Settings.SizeKind;
        var parentSize = node.Parent?.Size(kind) ?? 0;
        _share.Width = parentSize > 0 ? ShareWidth * node.Size(kind) / parentSize : 0;
        Paint();
    }

    public void Paint()
    {
        if (Node is not { } node)
        {
            return;
        }

        var colors = AppTheme.Colors;
        var basket = _explore.Session.Basket;
        var marked = basket.IsMarked(node);
        var covered = !marked && basket.IsCovered(node);
        var inside = marked || covered ? 0 : basket.MarkedInside(node, _explore.Settings.SizeKind);
        var filled = marked || covered || inside > 0;
        var shown = filled || _explore.CanMark(node);

        _hit.Opacity = !shown ? 0 : covered ? States.Disabled : 1;
        _hit.IsHitTestVisible = shown;
        _box.Background = filled ? AppTheme.Brush(colors.Accent) : Brushes.Transparent;
        _box.BorderBrush = AppTheme.Brush(filled ? colors.Accent : colors.TextFaint);
        _check.IsVisible = marked || covered;
        _mixed.IsVisible = inside > 0;

        _tile.Background = AppTheme.Brush(marked ? colors.Accent : colors.SurfaceAlt);
        var ink = AppTheme.Brush(marked ? colors.OnAccent : colors.TextMuted);
        _folderGlyph.Stroke = ink;
        _extension.Foreground = ink;
        _share.Background = AppTheme.Brush(marked ? colors.Accent : colors.TextFaint);

        var describe = _explore.Describe(node);
        _meta.Text = inside > 0 ? $"{describe} · {Strings.Get("explore.marked", _explore.Bytes(inside))}" : describe;

        Background = marked ? AppTheme.Brush(colors.Accent, States.Selected)
            : _explore.Session.Hovered == node ? AppTheme.Brush(colors.Accent, States.Hover)
            : Brushes.Transparent;
    }
}
