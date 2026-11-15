using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Map;

public sealed record Bubble(FileNode? Node, Circle Circle, long Size, int Count, IReadOnlyList<Bubble> Inner);

public sealed record MapLayout(Circle Outer, IReadOnlyList<Bubble> Bubbles)
{
    public const int MaxBubbles = 36;

    public const int MaxInner = 12;

    public const double MinShare = 0.01;

    public const double MinRadius = 10;

    public const double RingInset = 28;

    public const double ChildInset = 14;

    public const double InnerRadius = 38;

    public const double InnerScale = 0.84;

    private const double Gap = 0.025;

    public static readonly MapLayout Empty = new(new Circle(0, 0, 0), []);

    public Bubble? Hit(double x, double y) =>
        Bubbles.LastOrDefault(bubble => bubble.Circle.Distance(x, y) <= bubble.Circle.Radius);

    public static MapLayout Build(FileNode folder, SizeKind kind, bool showHidden, double width, double height)
    {
        var outer = new Circle(width / 2, height / 2, Math.Max(0, Math.Min(width, height) / 2 - RingInset));
        var area = outer with { Radius = Math.Max(0, outer.Radius - ChildInset) };
        var bubbles = Pack(folder, kind, showHidden, area, MaxBubbles)
            .Select(bubble => bubble.Node is { IsDirectory: true } node && bubble.Circle.Radius >= InnerRadius
                ? bubble with
                {
                    Inner = Pack(node, kind, showHidden,
                            bubble.Circle with { Radius = bubble.Circle.Radius * InnerScale }, MaxInner)
                        .Where(inner => inner.Node is not null)
                        .ToList()
                }
                : bubble)
            .ToList();
        return new MapLayout(outer, bubbles);
    }

    private static List<Bubble> Pack(FileNode folder, SizeKind kind, bool showHidden, Circle area, int limit)
    {
        var total = folder.Size(kind);
        if (total <= 0 || area.Radius <= 0)
        {
            return [];
        }

        var minimum = Math.Max(total * MinShare, total * Math.Pow(MinRadius / area.Radius, 2));
        var shown = folder.Children
            .Where(child => showHidden || !child.IsHidden)
            .Select(child => (Node: child, Size: child.Size(kind)))
            .Where(entry => entry.Size > 0)
            .OrderByDescending(entry => entry.Size)
            .Take(limit)
            .TakeWhile(entry => entry.Size >= minimum)
            .ToList();

        var entries = shown.Select(entry => (Node: (FileNode?)entry.Node, entry.Size, Count: 1)).ToList();
        var rest = total - shown.Sum(entry => entry.Size);
        if (rest > 0)
        {
            entries.Add((null, rest, folder.Children.Count - shown.Count));
        }

        var unit = Math.Sqrt(total);
        var (circles, radius) = CirclePacking.Pack(
            entries.Select(entry => Math.Sqrt(entry.Size) / unit).ToList(), Gap);
        var scale = radius == 0 ? 0 : area.Radius / radius;
        return entries.Select((entry, index) => new Bubble(
            entry.Node,
            new Circle(area.X + circles[index].X * scale, area.Y + circles[index].Y * scale,
                circles[index].Radius * scale),
            entry.Size,
            entry.Count,
            [])).ToList();
    }
}
