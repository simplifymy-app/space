using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class AboutBanner : Border
{
    private const double BandHeight = 148;
    private const double LogoScale = 1.45;

    public AboutBanner()
    {
        Height = BandHeight;
        CornerRadius = new CornerRadius(Radius.Panel);
        BorderThickness = new Thickness(Sizes.Hairline);
        ClipToBounds = true;
        Ui.Paint(this, BackgroundProperty, Ink.Surface);
        Ui.Paint(this, BorderBrushProperty, Ink.Hairline);

        var tagline = Ui.Text(Strings.Get("settings.tagline"), Typography.Section, Ink.Text);
        tagline.Margin = new Thickness(0, Space.RowGap, 0, 0);
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(Space.Section, 0, 0, 0),
            Children = { new BrandLogo(LogoScale), tagline }
        };

        Child = new Panel { Children = { new FileTiles(0.42), stack } };
    }
}
