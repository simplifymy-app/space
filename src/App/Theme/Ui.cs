using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Theme;

public enum Ink
{
    Text,
    TextMuted,
    TextFaint,
    Accent,
    OnAccent,
    Danger,
    Background,
    Surface,
    SurfaceAlt,
    Hairline,
    AccentHover,
    AccentSelected
}

public static class Ui
{
    public static TextBlock Text(string text, TypeRole role, Ink? ink = Ink.Text)
    {
        var block = new TextBlock
        {
            Text = role.Uppercase ? text.ToUpperInvariant() : text,
            FontSize = role.Size,
            FontWeight = (FontWeight)role.Weight,
            LetterSpacing = role.Tracking * role.Size,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Bind(block, TextBlock.FontFamilyProperty, role.Mono ? "FontMono" : "FontSans");
        return ink is { } bound ? Paint(block, TextBlock.ForegroundProperty, bound) : block;
    }

    public static TextBlock Section(string text) => Text(text, Typography.Section, Ink.TextFaint);

    public static T Paint<T>(T control, AvaloniaProperty property, Ink ink) where T : Control
    {
        Bind(control, property, ink.ToString());
        return control;
    }

    public static Border Surface(Ink ink, Control? child = null)
    {
        var border = new Border { Child = child };
        return Paint(border, Border.BackgroundProperty, ink);
    }

    public static Border Hairline(bool vertical = false)
    {
        var line = vertical
            ? new Border { Width = Sizes.Hairline }
            : new Border { Height = Sizes.Hairline };
        return Paint(line, Border.BackgroundProperty, Ink.Hairline);
    }

    public static Button Button(string key, Action click, string? style = null)
    {
        var button = new Button { Content = Strings.Get(key) };
        if (style is not null)
        {
            button.Classes.Add(style);
        }

        button.Click += (_, _) => click();
        return button;
    }

    public static void Follow(Control control, Action repaint)
    {
        AppTheme.Changed += repaint;
        control.DetachedFromVisualTree += (_, _) => AppTheme.Changed -= repaint;
        repaint();
    }

    private static void Bind(Control control, AvaloniaProperty property, string key) =>
        control.Bind(property, control.GetResourceObservable(key));
}
