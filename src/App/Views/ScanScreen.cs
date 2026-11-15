using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class ScanScreen : Grid
{
    private readonly Explore _explore;
    private readonly TextBlock _path = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly TextBlock _counters = Ui.Text(string.Empty, Typography.Value, Ink.TextMuted);
    private readonly TextBlock _unreadable = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
    private readonly StackPanel _unreadableRow;
    private readonly UsageBar _progress = new() { Width = 320, Highlight = true };

    public ScanScreen(Explore explore)
    {
        _explore = explore;
        Ui.Paint(this, BackgroundProperty, Ink.Background);

        var stop = new Button
        {
            Height = 36,
            Classes = { "secondary" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = Glyph.Label(Icons.Stop, Ui.Text(Strings.Get("scan.stop"), Typography.Label))
        };
        stop.Click += (_, _) => explore.Stop();

        _unreadableRow = Glyph.Label(Icons.Lock, _unreadable, Ink.TextMuted);
        foreach (var centered in new Control[] { _path, _counters, _unreadableRow, _progress })
        {
            centered.HorizontalAlignment = HorizontalAlignment.Center;
        }

        _path.MaxWidth = 560;
        var title = Ui.Text(Strings.Get("scan.title"), Typography.Headline);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        var section = Ui.Section(Strings.Get("scan.section", explore.RootName));
        section.HorizontalAlignment = HorizontalAlignment.Center;

        Children.Add(new StackPanel
        {
            Spacing = Space.Inset,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new ScanLoader { Margin = new Thickness(0, 0, 0, Space.Section) },
                section,
                title,
                _path,
                _counters,
                _progress,
                _unreadableRow,
                Spaced(stop)
            }
        });
        Update();
    }

    public void Update()
    {
        var share = _explore.Share;
        var percent = _explore.IsVolume ? $" · {Format.Percent(share)}" : string.Empty;
        _path.Text = Format.MiddleTruncate(_explore.Session.Scanner.Current, 80);
        _counters.Text = $"{_explore.Bytes(_explore.Counted)} · " +
                         $"{Strings.Get("explore.items", Format.Count(_explore.Session.Root.Items))} · " +
                         $"{Format.Elapsed(_explore.Clock.Elapsed)}{percent}";
        _progress.IsVisible = _explore.IsVolume;
        _progress.Show(1000, (long)(share * 1000), 0);
        var unreadable = _explore.Session.Scanner.Unreadable;
        _unreadable.Text = Strings.Get("explore.unreadable", Format.Count(unreadable));
        _unreadableRow.Opacity = unreadable > 0 ? 1 : 0;
    }

    private static Control Spaced(Control control)
    {
        control.Margin = new Thickness(0, Space.Base, 0, 0);
        return control;
    }
}

public sealed class ScanLoader : Control
{
    private const double Size = 200;
    private const double Platter = Size * 0.27;
    private const double Lens = Size * 0.15;
    private const double Orbit = 3.2;
    private const double Ripple = 2.4;
    private const int Ripples = 3;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _frames = new() { Interval = TimeSpan.FromMilliseconds(33) };

    public ScanLoader()
    {
        Width = Size;
        Height = Size;
        HorizontalAlignment = HorizontalAlignment.Center;
        _frames.Tick += (_, _) =>
        {
            if (IsEffectivelyVisible)
            {
                InvalidateVisual();
            }
        };
        AttachedToVisualTree += (_, _) => _frames.Start();
        DetachedFromVisualTree += (_, _) => _frames.Stop();
    }

    public override void Render(DrawingContext context)
    {
        var colors = AppTheme.Colors;
        var seconds = _clock.Elapsed.TotalSeconds;
        var center = new Point(Size / 2, Size / 2);

        for (var index = 0; index < Ripples; index++)
        {
            var phase = (seconds / Ripple + (double)index / Ripples) % 1;
            var radius = Platter + (Size / 2 - Platter) * phase;
            context.DrawEllipse(null, new Pen(AppTheme.Brush(colors.Accent, 0.22 * (1 - phase)), 1.5), center,
                radius, radius);
        }

        var platter = new Pen(AppTheme.Brush(colors.TextMuted), 8);
        context.DrawEllipse(AppTheme.Brush(colors.Surface), platter, center, Platter, Platter);
        context.DrawEllipse(AppTheme.Brush(colors.TextMuted), null, center, 5, 5);

        var angle = seconds * Math.Tau / Orbit - Math.PI / 2;
        var lens = center + new Vector(Math.Cos(angle), Math.Sin(angle)) * Platter * 0.85;
        var accent = new Pen(AppTheme.Brush(colors.Accent), 8, lineCap: PenLineCap.Round);
        var reach = Lens * Math.Sqrt(0.5);
        context.DrawLine(new Pen(AppTheme.Brush(colors.Accent), 11, lineCap: PenLineCap.Round),
            lens + new Vector(reach + 3, reach + 3), lens + new Vector(reach + 15, reach + 15));
        context.DrawEllipse(AppTheme.Brush(colors.Background), accent, lens, Lens, Lens);
        context.DrawGeometry(null, new Pen(AppTheme.Brush(colors.Accent), 3.5, lineCap: PenLineCap.Round),
            Glint(lens));
    }

    private static Geometry Glint(Point lens)
    {
        var radius = Lens * 0.55;
        var start = lens + new Vector(Math.Cos(Math.PI * 1.15), Math.Sin(Math.PI * 1.15)) * radius;
        var end = lens + new Vector(Math.Cos(Math.PI * 1.45), Math.Sin(Math.PI * 1.45)) * radius;
        var geometry = new StreamGeometry();
        using (var draw = geometry.Open())
        {
            draw.BeginFigure(start, false);
            draw.ArcTo(end, new Avalonia.Size(radius, radius), 0, false, SweepDirection.Clockwise);
            draw.EndFigure(false);
        }

        return geometry;
    }
}
