using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Map;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class MapView : Control
{
    private const double NameRadius = 48;
    private const double SizeRadius = 30;
    private const double Badge = 10;
    private const double InnerBadge = 7;
    private const double PillHeight = 28;

    private static readonly Geometry CheckMark = Geometry.Parse(Icons.Check.Path);

    private readonly Explore _explore;
    private MapLayout _layout = MapLayout.Empty;
    private FileNode? _folder;

    public MapView(Explore explore)
    {
        _explore = explore;
        ClipToBounds = true;
        Ui.Follow(this, InvalidateVisual);
    }

    public void Show(FileNode folder)
    {
        _folder = folder;
        Relayout();
    }

    public void Relayout()
    {
        if (_folder is null)
        {
            return;
        }

        var settings = _explore.Settings;
        _layout = MapLayout.Build(_folder, settings.SizeKind, settings.ShowHidden, Bounds.Width, Bounds.Height);
        InvalidateVisual();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Relayout();
    }

    public override void Render(DrawingContext context)
    {
        var colors = AppTheme.Colors;
        var hairline = new Pen(AppTheme.Brush(colors.Hairline));
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        DrawCircle(context, _layout.Outer, AppTheme.Brush(colors.Background), hairline);
        foreach (var bubble in _layout.Bubbles)
        {
            DrawBubble(context, bubble, hairline);
        }

        DrawPill(context);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var bubble = Hit(e);
        _explore.Session.Hover(bubble?.Node);
        ToolTip.SetTip(this, bubble?.Node is { } node ? Tip(node) : null);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _explore.Session.Hover(null);
        ToolTip.SetTip(this, null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Hit(e)?.Node is not { } node)
        {
            return;
        }

        _explore.Session.Select(node);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _explore.Menu(this, node).Open(this);
        }
        else if (e.ClickCount == 2)
        {
            _explore.Activate(node);
        }

        e.Handled = true;
    }

    private Bubble? Hit(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        return _layout.Hit(point.X, point.Y);
    }

    private string Tip(FileNode node)
    {
        var kind = _explore.Settings.SizeKind;
        var parent = node.Parent?.Size(kind) ?? 0;
        var share = parent > 0 ? (double)node.Size(kind) / parent : 0;
        var items = node.IsDirectory ? $" · {Format.Count(node.Items)}" : string.Empty;
        return $"{node.Name}\n{node.Path}\n{_explore.Size(node)}{items} · {share.ToString("P1", CultureInfo.InvariantCulture)}";
    }

    private void DrawBubble(DrawingContext context, Bubble bubble, Pen hairline)
    {
        var colors = AppTheme.Colors;
        var session = _explore.Session;
        var circle = bubble.Circle;
        if (bubble.Node is not { } node)
        {
            DrawCircle(context, circle, null, new Pen(AppTheme.Brush(colors.Hairline), 1, new DashStyle([4, 3], 0)));
            if (circle.Radius >= SizeRadius)
            {
                DrawLines(context, circle, Strings.Get("explore.items", Format.Count(bubble.Count)),
                    _explore.Bytes(bubble.Size), Typography.Section, colors.TextMuted);
            }

            return;
        }

        var basket = session.Basket;
        var marked = basket.IsMarked(node) || basket.IsCovered(node);
        DrawCircle(context, circle, AppTheme.Brush(colors.Surface), hairline);
        if (marked)
        {
            DrawCircle(context, circle, AppTheme.Brush(colors.Accent, States.Selected), null);
        }

        foreach (var inner in bubble.Inner)
        {
            DrawCircle(context, inner.Circle, AppTheme.Brush(colors.SurfaceAlt), hairline);
            if (!marked && inner.Node is { } innerNode && basket.IsMarked(innerNode))
            {
                DrawCircle(context, inner.Circle, AppTheme.Brush(colors.Accent, 0.32), null);
                DrawBadge(context, inner.Circle, InnerBadge);
            }
        }

        if (session.Hovered == node)
        {
            DrawCircle(context, circle, AppTheme.Brush(colors.Accent, States.Hover), null);
        }

        if (session.Selected == node)
        {
            DrawCircle(context, circle, null, new Pen(AppTheme.Brush(colors.Text), 1.5));
        }

        if (circle.Radius >= NameRadius)
        {
            DrawLines(context, circle, node.Name, _explore.Size(node), Typography.Label, colors.Text);
        }
        else if (circle.Radius >= SizeRadius)
        {
            DrawLines(context, circle, null, _explore.Size(node), Typography.Meta, colors.TextMuted);
        }

        if (marked)
        {
            DrawBadge(context, circle, Badge);
        }
    }

    private void DrawPill(DrawingContext context)
    {
        if (_folder is null || _layout.Outer.Radius <= 0)
        {
            return;
        }

        var colors = AppTheme.Colors;
        var name = Text(_folder.Parent is null ? _folder.Path : _folder.Name, AppTheme.Sans, 13, FontWeight.Medium,
            colors.Text, 260);
        var size = Text(_explore.Size(_folder), AppTheme.Mono, Typography.Value.Size, FontWeight.Normal,
            colors.TextMuted, 160);
        var width = name.Width + size.Width + Space.Base + 20;
        var top = _layout.Outer.Y - _layout.Outer.Radius - PillHeight / 2;
        var rect = new Rect(_layout.Outer.X - width / 2, top, width, PillHeight);
        context.DrawRectangle(AppTheme.Brush(colors.Background), new Pen(AppTheme.Brush(colors.Hairline)), rect,
            Radius.Control, Radius.Control);
        context.DrawText(name, new Point(rect.X + 10, rect.Center.Y - name.Height / 2));
        context.DrawText(size, new Point(rect.Right - 10 - size.Width, rect.Center.Y - size.Height / 2));
    }

    private static void DrawLines(DrawingContext context, Circle circle, string? title, string meta, TypeRole role,
        Rgb titleInk)
    {
        var width = circle.Radius * 1.6;
        var metaRole = role == Typography.Label ? Typography.Value : role;
        var metaText = Text(meta, AppTheme.Mono, metaRole.Size, (FontWeight)metaRole.Weight,
            AppTheme.Colors.TextMuted, width);
        var titleText = title is null
            ? null
            : Text(title, role.Mono ? AppTheme.Mono : AppTheme.Sans, role.Size, (FontWeight)role.Weight, titleInk, width);
        var top = circle.Y - (metaText.Height + (titleText?.Height ?? 0)) / 2;
        if (titleText is not null)
        {
            context.DrawText(titleText, new Point(circle.X - titleText.Width / 2, top));
            top += titleText.Height;
        }

        context.DrawText(metaText, new Point(circle.X - metaText.Width / 2, top));
    }

    private static void DrawBadge(DrawingContext context, Circle circle, double radius)
    {
        var offset = circle.Radius * Math.Sqrt(0.5);
        var badge = new Circle(circle.X + offset, circle.Y - offset, radius);
        DrawCircle(context, badge, AppTheme.Brush(AppTheme.Colors.Accent), null);
        var scale = radius * 1.4 / Sizes.Icon;
        using (context.PushTransform(Matrix.CreateTranslation(-Sizes.Icon / 2, -Sizes.Icon / 2)
                                     * Matrix.CreateScale(scale, scale)
                                     * Matrix.CreateTranslation(badge.X, badge.Y)))
        {
            context.DrawGeometry(null, new Pen(AppTheme.Brush(AppTheme.Colors.OnAccent), 2 / scale,
                lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), CheckMark);
        }
    }

    private static void DrawCircle(DrawingContext context, Circle circle, IBrush? fill, Pen? pen) =>
        context.DrawEllipse(fill, pen, new Point(circle.X, circle.Y), circle.Radius, circle.Radius);

    private static FormattedText Text(string text, FontFamily family, double size, FontWeight weight, Rgb ink,
        double width) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(family, FontStyle.Normal, weight),
            size, AppTheme.Brush(ink))
        {
            MaxTextWidth = Math.Max(1, width),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };
}
