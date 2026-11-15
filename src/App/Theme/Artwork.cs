using System.Globalization;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace SimplifyMySpace.App.Theme;

public sealed class Artwork : Control
{
    public static readonly Artwork.Drawing SpaceDuck = Drawing.Load("avares://SimplifyMySpace/Assets/Art/space-duck.svg");

    private readonly Drawing _drawing;

    public Artwork(Drawing drawing, double height)
    {
        _drawing = drawing;
        Height = height;
        Width = height * drawing.ViewBox.Width / drawing.ViewBox.Height;
    }

    public override void Render(DrawingContext context) => _drawing.Draw(context, new Rect(Bounds.Size));

    public sealed record Shape(Geometry Geometry, IBrush? Fill, IPen? Pen);

    public sealed class Drawing(Rect viewBox, IReadOnlyList<Shape> shapes)
    {
        public Rect ViewBox { get; } = viewBox;

        public static Drawing Load(string uri)
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            var root = XDocument.Load(stream).Root!;
            var box = Numbers(root.Attribute("viewBox")!.Value);
            var shapes = new List<Shape>();
            Collect(root, new Dictionary<string, string>(), shapes);
            return new Drawing(new Rect(box[0], box[1], box[2], box[3]), shapes);
        }

        public void Draw(DrawingContext context, Rect target)
        {
            var scale = Math.Min(target.Width / ViewBox.Width, target.Height / ViewBox.Height);
            var offset = new Vector(
                target.X + (target.Width - ViewBox.Width * scale) / 2 - ViewBox.X * scale,
                target.Y + (target.Height - ViewBox.Height * scale) / 2 - ViewBox.Y * scale);
            using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offset)))
            {
                foreach (var shape in shapes)
                {
                    context.DrawGeometry(shape.Fill, shape.Pen, shape.Geometry);
                }
            }
        }

        private static void Collect(XElement element, Dictionary<string, string> inherited, List<Shape> shapes)
        {
            foreach (var child in element.Elements())
            {
                var style = new Dictionary<string, string>(inherited);
                foreach (var attribute in child.Attributes())
                {
                    style[attribute.Name.LocalName] = attribute.Value;
                }

                if (child.Name.LocalName == "g")
                {
                    Collect(child, style, shapes);
                }
                else if (Geometry(child.Name.LocalName, style) is { } geometry)
                {
                    shapes.Add(new Shape(geometry, Fill(style), Stroke(style)));
                }
            }
        }

        private static Geometry? Geometry(string name, Dictionary<string, string> style)
        {
            double Number(string key) => style.TryGetValue(key, out var value)
                ? double.Parse(value, CultureInfo.InvariantCulture)
                : 0;

            return name switch
            {
                "rect" => new RectangleGeometry(new Rect(Number("x"), Number("y"), Number("width"), Number("height")),
                    Number("rx"), Number("rx")),
                "ellipse" => new EllipseGeometry(new Rect(Number("cx") - Number("rx"), Number("cy") - Number("ry"),
                    Number("rx") * 2, Number("ry") * 2)),
                "circle" => new EllipseGeometry(new Rect(Number("cx") - Number("r"), Number("cy") - Number("r"),
                    Number("r") * 2, Number("r") * 2)),
                "path" => StreamGeometry.Parse(style["d"]),
                _ => null
            };
        }

        private static IBrush? Fill(Dictionary<string, string> style)
        {
            var fill = style.GetValueOrDefault("fill", "#000");
            return Brush(fill, Opacity(style, "opacity") * Opacity(style, "fill-opacity"));
        }

        private static IPen? Stroke(Dictionary<string, string> style)
        {
            if (!style.TryGetValue("stroke", out var stroke)
                || Brush(stroke, Opacity(style, "opacity") * Opacity(style, "stroke-opacity")) is not { } brush)
            {
                return null;
            }

            var width = style.TryGetValue("stroke-width", out var value)
                ? double.Parse(value, CultureInfo.InvariantCulture)
                : 1;
            var cap = style.GetValueOrDefault("stroke-linecap") == "round" ? PenLineCap.Round : PenLineCap.Flat;
            var join = style.GetValueOrDefault("stroke-linejoin") == "round" ? PenLineJoin.Round : PenLineJoin.Miter;
            return new Pen(brush, width, lineCap: cap, lineJoin: join);
        }

        private static double Opacity(Dictionary<string, string> style, string key) =>
            style.TryGetValue(key, out var value) ? double.Parse(value, CultureInfo.InvariantCulture) : 1;

        private static IBrush? Brush(string value, double opacity)
        {
            if (value == "none")
            {
                return null;
            }

            if (value.StartsWith("rgba(", StringComparison.Ordinal))
            {
                var parts = Numbers(value[5..^1]);
                return new SolidColorBrush(Color.FromRgb((byte)parts[0], (byte)parts[1], (byte)parts[2]),
                    parts[3] * opacity);
            }

            return new SolidColorBrush(Color.Parse(value), opacity);
        }

        private static double[] Numbers(string text) =>
            text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => double.Parse(part, CultureInfo.InvariantCulture))
                .ToArray();
    }
}
