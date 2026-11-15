using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SimplifyMySpace.App.Theme;

public sealed class LogoMark : Panel
{
    public const string Product = "SPACE";

    private const double TagOpticalTrim = 0.76;

    public LogoMark(double scale = 1)
    {
        ClipToBounds = false;
        Height = 38 * scale;

        var word = new TextBlock
        {
            Text = "Simplify",
            FontWeight = FontWeight.SemiBold,
            FontSize = 18 * scale,
            LetterSpacing = -0.03 * 18 * scale
        };

        var tag = new TextBlock
        {
            Text = Product,
            FontWeight = FontWeight.Medium,
            FontSize = 8 * scale,
            LetterSpacing = 0.22 * 8 * scale,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 2 * scale, -TagOpticalTrim * scale, 0)
        };

        var my = new TextBlock
        {
            Text = "my",
            FontFamily = AppTheme.Hand,
            FontWeight = FontWeight.SemiBold,
            FontSize = 21 * scale,
            RenderTransform = new RotateTransform(-7),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(5 * scale, 0, 0, 0),
            IsHitTestVisible = false
        };

        word.FontFamily = AppTheme.Latin;
        tag.FontFamily = AppTheme.Mono;
        Ui.Paint(word, TextBlock.ForegroundProperty, Ink.Text);
        Ui.Paint(tag, TextBlock.ForegroundProperty, Ink.TextMuted);
        Ui.Paint(my, TextBlock.ForegroundProperty, Ink.Accent);

        Children.Add(new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Children = { word, tag }
        });
        Children.Add(my);
    }
}

public sealed class BrandLogo : StackPanel
{
    public BrandLogo(double scale = 1)
    {
        Orientation = Orientation.Horizontal;
        Spacing = 10 * scale * 18 / 32;
        Children.Add(new Artwork(Artwork.SpaceDuck, 18 * scale * 56 / 32) { VerticalAlignment = VerticalAlignment.Top });
        Children.Add(new LogoMark(scale) { VerticalAlignment = VerticalAlignment.Top });
    }
}
