using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SimplifyMySpace.App.Theme;

public sealed class AppMark : Control
{
    private const double Grid = 256;

    private static readonly Geometry Glint = Geometry.Parse("M138 82 A26 26 0 0 1 166 74");

    public AppMark(double size)
    {
        Width = size;
        Height = size;
        Ui.Follow(this, InvalidateVisual);
    }

    public static WindowIcon WindowIcon() => new(Bitmap(512));

    public static RenderTargetBitmap Bitmap(int pixels)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(pixels, pixels), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            Draw(context, pixels);
        }

        return bitmap;
    }

    public override void Render(DrawingContext context) => Draw(context, Bounds.Width);

    private static void Draw(DrawingContext context, double size)
    {
        var tile = AppTheme.Brush(AppTheme.Colors.Accent);
        var mark = AppTheme.Brush(AppTheme.Colors.OnAccent);
        using (context.PushTransform(Matrix.CreateScale(size / Grid, size / Grid)))
        {
            context.DrawRectangle(tile, null, new Rect(0, 0, Grid, Grid), 57, 57);
            var stroke = new Pen(mark, 14);
            context.DrawEllipse(null, stroke, new Point(108, 140), 62, 62);
            context.DrawEllipse(mark, null, new Point(108, 140), 11, 11);
            context.DrawEllipse(tile, stroke, new Point(158, 98), 44, 44);
            context.DrawLine(new Pen(mark, 20, lineCap: PenLineCap.Round), new Point(190, 130), new Point(214, 154));
            context.DrawGeometry(null, new Pen(mark, 8, lineCap: PenLineCap.Round), Glint);
        }
    }
}
