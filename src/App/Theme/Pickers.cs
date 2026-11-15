using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Theme;

public sealed class Segmented<T> : Border where T : notnull
{
    private readonly Dictionary<T, (Border Segment, TextBlock Label)> _parts = [];
    private T _value;

    public Segmented(IEnumerable<T> options, T value, Func<T, string> caption, Action<T> changed)
    {
        _value = value;
        Height = 32;
        Padding = new Thickness(2);
        CornerRadius = new CornerRadius(Radius.Control);
        Ui.Paint(this, BackgroundProperty, Ink.SurfaceAlt);

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        foreach (var option in options)
        {
            var label = Ui.Text(caption(option), Typography.Label);
            label.FontSize = 13;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            var segment = new Border
            {
                MinWidth = 44,
                Padding = new Thickness(10, 0),
                CornerRadius = new CornerRadius(Radius.Control),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = label
            };

            segment.PointerPressed += (_, _) =>
            {
                _value = option;
                Repaint();
                changed(option);
            };

            _parts[option] = (segment, label);
            row.Children.Add(segment);
        }

        Child = row;
        Ui.Follow(this, Repaint);
    }

    private void Repaint()
    {
        foreach (var (option, (segment, label)) in _parts)
        {
            var active = Equals(option, _value);
            segment.Background = active ? AppTheme.Brush(AppTheme.Colors.Accent) : Brushes.Transparent;
            label.Foreground = AppTheme.Brush(active ? AppTheme.Colors.OnAccent : AppTheme.Colors.TextMuted);
        }
    }
}

public sealed class Toggle : Border
{
    private readonly Ellipse _knob;
    private bool _value;

    public Toggle(bool value, Action<bool> changed)
    {
        _value = value;
        Width = Sizes.ToggleWidth;
        Height = Sizes.ToggleHeight;
        CornerRadius = new CornerRadius(Sizes.ToggleHeight / 2);
        Cursor = new Cursor(StandardCursorType.Hand);
        _knob = new Ellipse
        {
            Width = Sizes.ToggleKnob,
            Height = Sizes.ToggleKnob,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Transitions =
            [
                new ThicknessTransition { Property = MarginProperty, Duration = TimeSpan.FromMilliseconds(Motion.State) }
            ]
        };

        Child = _knob;
        PointerPressed += (_, _) =>
        {
            _value = !_value;
            Repaint();
            changed(_value);
        };

        Ui.Follow(this, Repaint);
    }

    private void Repaint()
    {
        var inset = (Sizes.ToggleHeight - Sizes.ToggleKnob) / 2;
        var travel = Sizes.ToggleWidth - Sizes.ToggleKnob - inset * 2;
        _knob.Margin = new Thickness(_value ? inset + travel : inset, 0, 0, 0);
        _knob.Fill = AppTheme.Brush(_value ? AppTheme.Colors.OnAccent : AppTheme.Colors.TextFaint);
        Background = AppTheme.Brush(_value ? AppTheme.Colors.Accent : AppTheme.Colors.SurfaceAlt);
    }
}

public sealed class SwatchRow : StackPanel
{
    private readonly Dictionary<AccentPreset, (Border Cell, Ellipse Dot)> _cells = [];
    private AccentPreset _value;

    public SwatchRow(AccentPreset value, Action<AccentPreset> changed)
    {
        _value = value;
        Orientation = Orientation.Horizontal;
        Spacing = 6;
        foreach (var preset in Enum.GetValues<AccentPreset>())
        {
            var dot = new Ellipse { Width = 20, Height = 20 };
            var cell = new Border
            {
                Width = 30,
                Height = 30,
                Padding = new Thickness(3),
                CornerRadius = new CornerRadius(15),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = dot
            };

            cell.PointerPressed += (_, _) =>
            {
                _value = preset;
                Repaint();
                changed(preset);
            };

            _cells[preset] = (cell, dot);
            Children.Add(cell);
        }

        Ui.Follow(this, Repaint);
    }

    private void Repaint()
    {
        foreach (var (preset, (cell, dot)) in _cells)
        {
            dot.Fill = AppTheme.Brush(Accents.Fill(preset, AppTheme.Colors.IsLight));
            cell.BorderThickness = new Thickness(preset == _value ? 2 : 0);
            cell.BorderBrush = AppTheme.Brush(AppTheme.Colors.Text);
        }
    }
}

public sealed class UsageBar : Control
{
    private double _used;
    private double _freed;
    private bool _highlight;

    public UsageBar()
    {
        Height = Sizes.Bar;
        Ui.Follow(this, InvalidateVisual);
    }

    public bool Highlight
    {
        get => _highlight;
        set
        {
            _highlight = value;
            InvalidateVisual();
        }
    }

    public void Show(long total, long used, long freed)
    {
        _used = total > 0 ? Math.Clamp((double)used / total, 0, 1) : 0;
        _freed = total > 0 ? Math.Clamp((double)freed / total, 0, _used) : 0;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var colors = AppTheme.Colors;
        var width = Bounds.Width;
        var height = Bounds.Height;
        var radius = height / 2;
        context.DrawRectangle(AppTheme.Brush(colors.SurfaceAlt), null, new Rect(0, 0, width, height), radius, radius);
        context.DrawRectangle(AppTheme.Brush(_highlight ? colors.Accent : colors.TextMuted), null,
            new Rect(0, 0, width * _used, height), radius, radius);
        if (_freed > 0)
        {
            context.DrawRectangle(AppTheme.Brush(colors.Accent), null,
                new Rect(width * (_used - _freed), 0, width * _freed, height), radius, radius);
        }
    }
}
