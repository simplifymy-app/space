using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class FileTiles : Control
{
    private const double Tilt = -13;
    private const double TileShare = 0.42;
    private const double PitchRatio = 1.2;
    private const double FadeShare = 0.16;

    private static readonly Rgb[] Palette =
    [
        Rgb.Hex(0x6E8BFF),
        Rgb.Hex(0xE0A458),
        Rgb.Hex(0x5FBF8F),
        Rgb.Hex(0xD96A8B),
        Rgb.Hex(0xA98BFF)
    ];

    private static readonly (Geometry Glyph, string Size)[] Files =
    [
        (Glyph("M3 6.5V15a1 1 0 001 1h12a1 1 0 001-1V8a1 1 0 00-1-1h-6L8.5 5H4a1 1 0 00-1 1.5z"), "48.2 GB"),
        (Glyph("M3.5 5h8.5a1 1 0 011 1v8a1 1 0 01-1 1H3.5a1 1 0 01-1-1V6a1 1 0 011-1zM13 8.5L17.5 6v8L13 11.5"), "12.4 GB"),
        (Glyph("M3.5 4.5h13v3h-13zM4.5 7.5V15a1 1 0 001 1h9a1 1 0 001-1V7.5M8 11h4"), "3.1 GB"),
        (Glyph("M10 2.8l6.5 3.4v7.6L10 17.2l-6.5-3.4V6.2zM3.5 6.2L10 9.6l6.5-3.4M10 9.6v7.6"), "6.8 GB"),
        (Glyph("M3 15V5.5a1 1 0 011-1h4.5L10 6h5a1 1 0 011 1v1.5M3 15l2.2-5.8a1 1 0 01.9-.7H17l-2.3 6.2a1 1 0 01-.9.6H4a1 1 0 01-1-1"), "220 GB"),
        (Glyph("M5.5 3h6l3.5 3.5V16a1 1 0 01-1 1h-8.5a1 1 0 01-1-1V4a1 1 0 011-1zM11.5 3v3.5H15M7.5 10.5h5M7.5 13.5h5"), "84 MB"),
        (Glyph("M4.5 4h11a1.5 1.5 0 011.5 1.5v9a1.5 1.5 0 01-1.5 1.5h-11A1.5 1.5 0 013 14.5v-9A1.5 1.5 0 014.5 4zM3.5 13.5l4-4 3 3 2-2 4 4M12 7.5a1 1 0 1 0 2 0a1 1 0 1 0 -2 0"), "4.2 MB"),
        (Glyph("M8 14.5V4.5l8-1.5v10M4 14.5a2 2 0 1 0 4 0a2 2 0 1 0 -4 0M12 13a2 2 0 1 0 4 0a2 2 0 1 0 -4 0"), "1.9 GB"),
        (Glyph("M4.5 3.5h11a1 1 0 011 1v11a1 1 0 01-1 1h-11a1 1 0 01-1-1v-11a1 1 0 011-1zM7 3.5v13M13 3.5v13M3.5 7H7M3.5 10H7M3.5 13H7M13 7h3.5M13 10h3.5M13 13h3.5"), "8.7 GB"),
        (Glyph("M10 3.5c3.6 0 6 1 6 2.3S13.6 8 10 8 4 7.1 4 5.8 6.4 3.5 10 3.5zM4 5.8v8.4c0 1.3 2.4 2.3 6 2.3s6-1 6-2.3V5.8M4 10c0 1.3 2.4 2.3 6 2.3s6-1 6-2.3"), "17.5 GB"),
        (Glyph("M5.5 3h6l3.5 3.5V16a1 1 0 01-1 1h-8.5a1 1 0 01-1-1V4a1 1 0 011-1zM11.5 3v3.5H15M8.5 10l-2 2 2 2M11.5 10l2 2-2 2"), "2.3 GB"),
        (Glyph("M6.5 6.5h9a1 1 0 011 1v8a1 1 0 01-1 1h-9a1 1 0 01-1-1v-8a1 1 0 011-1zM3.5 13.5V4.5a1 1 0 011-1h9M6 15l3-3 2.5 2.5 1.5-1.5 3 3"), "5.6 GB")
    ];

    public FileTiles(double start = 0.4)
    {
        Start = start;
        IsHitTestVisible = false;
        ClipToBounds = true;
        Ui.Follow(this, InvalidateVisual);
    }

    public double Start { get; }

    public override void Render(DrawingContext context) =>
        Draw(context, new Rect(Bounds.Size), AppTheme.Colors, Start);

    public static void Draw(DrawingContext context, Rect area, Palette colors, double start)
    {
        var tile = Math.Min(area.Height * TileShare, Math.Max(area.Width * 0.17, area.Height * 0.2));
        var pitch = tile * PitchRatio;
        var startX = area.X + area.Width * start;
        var fade = Math.Max(1, area.Width * FadeShare);
        var center = area.Center;
        var rotation = Matrix.CreateTranslation(-center.X, -center.Y)
                       * Matrix.CreateRotation(Tilt * Math.PI / 180)
                       * Matrix.CreateTranslation(center.X, center.Y);
        var reach = area.Width + area.Height * 2;
        var columns = (int)(reach / pitch) + 2;
        var rows = (int)((area.Height + area.Width * 0.5) / pitch) + 4;

        using (context.PushClip(area))
        using (context.PushTransform(rotation))
        {
            for (var column = 0; column < columns; column++)
            {
                var x = startX - area.Height + column * pitch;
                var phase = column * 0.37 % 1 * pitch * (column % 2 == 0 ? -1 : 1);
                for (var row = -2; row < rows; row++)
                {
                    var y = area.Y - area.Width * 0.25 + row * pitch + phase;
                    var screen = new Point(x + tile / 2, y + tile / 2).Transform(rotation);
                    var opacity = Math.Clamp((screen.X - startX) / fade, 0, 1);
                    if (opacity <= 0 || screen.Y < area.Top - tile || screen.Y > area.Bottom + tile)
                    {
                        continue;
                    }

                    var index = (column * 3 + row + 40) % Files.Length;
                    var color = Palette[(column * 2 + row + 40) % Palette.Length];
                    DrawTile(context, new Rect(x, y, tile, tile), Files[index], color, colors, opacity);
                }
            }
        }
    }

    private static void DrawTile(DrawingContext context, Rect rect, (Geometry Glyph, string Size) file, Rgb color,
        Palette colors, double opacity)
    {
        var radius = rect.Width * 0.22;
        var fill = color.Mix(colors.Background, colors.IsLight ? 0.55 : 0.62);
        var ink = colors.IsLight ? color.Mix(Rgb.Hex(0x000000), 0.35) : color.Mix(Rgb.Hex(0xFFFFFF), 0.45);
        context.DrawRectangle(AppTheme.Brush(fill, opacity), null, rect, radius, radius);

        var scale = rect.Width * 0.4 / Sizes.Icon;
        var origin = new Point(rect.Center.X - Sizes.Icon * scale / 2, rect.Y + rect.Height * 0.18);
        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(origin.X, origin.Y)))
        {
            context.DrawGeometry(null, new Pen(AppTheme.Brush(ink, opacity), 1.5, lineCap: PenLineCap.Round,
                lineJoin: PenLineJoin.Round), file.Glyph);
        }

        var size = Text(file.Size, rect.Width * 0.11, FontWeight.Normal, ink, opacity * 0.8);
        context.DrawText(size, new Point(rect.Center.X - size.Width / 2, rect.Bottom - rect.Height * 0.26));
    }

    private static Geometry Glyph(string path) => Geometry.Parse(path);

    private static FormattedText Text(string text, double size, FontWeight weight, Rgb ink, double opacity) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(AppTheme.Mono, FontStyle.Normal, weight), size, AppTheme.Brush(ink, opacity));
}
