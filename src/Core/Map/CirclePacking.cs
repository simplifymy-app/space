namespace SimplifyMySpace.Core.Map;

public readonly record struct Circle(double X, double Y, double Radius)
{
    public double Distance(double x, double y) => Math.Sqrt((X - x) * (X - x) + (Y - y) * (Y - y));

    public bool Overlaps(Circle other, double gap) =>
        Distance(other.X, other.Y) < Radius + other.Radius + gap - 1e-9;
}

public static class CirclePacking
{
    public static (IReadOnlyList<Circle> Circles, double Radius) Pack(IReadOnlyList<double> radii, double gap)
    {
        var placed = new List<Circle>(radii.Count);
        foreach (var radius in radii)
        {
            placed.Add(placed.Count switch
            {
                0 => new Circle(0, 0, radius),
                1 => new Circle(placed[0].Radius + radius + gap, 0, radius),
                _ => Nearest(placed, radius, gap)
            });
        }

        return Center(placed);
    }

    private static Circle Nearest(List<Circle> placed, double radius, double gap)
    {
        Circle? best = null;
        var bestReach = double.MaxValue;
        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                foreach (var candidate in Tangents(placed[i], placed[j], radius, gap))
                {
                    var reach = candidate.Distance(0, 0) + radius;
                    if (reach < bestReach && !placed.Any(circle => circle.Overlaps(candidate, gap)))
                    {
                        best = candidate;
                        bestReach = reach;
                    }
                }
            }
        }

        return best ?? new Circle(placed.Max(circle => circle.X + circle.Radius) + radius + gap, 0, radius);
    }

    private static IEnumerable<Circle> Tangents(Circle a, Circle b, double radius, double gap)
    {
        var reachA = a.Radius + radius + gap;
        var reachB = b.Radius + radius + gap;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance == 0 || distance > reachA + reachB || distance < Math.Abs(reachA - reachB))
        {
            yield break;
        }

        var along = (reachA * reachA - reachB * reachB + distance * distance) / (2 * distance);
        var across = Math.Sqrt(Math.Max(0, reachA * reachA - along * along));
        var baseX = a.X + along * dx / distance;
        var baseY = a.Y + along * dy / distance;
        yield return new Circle(baseX - across * dy / distance, baseY + across * dx / distance, radius);
        yield return new Circle(baseX + across * dy / distance, baseY - across * dx / distance, radius);
    }

    private static (IReadOnlyList<Circle>, double) Center(List<Circle> placed)
    {
        if (placed.Count == 0)
        {
            return (placed, 0);
        }

        var midX = (placed.Min(c => c.X - c.Radius) + placed.Max(c => c.X + c.Radius)) / 2;
        var midY = (placed.Min(c => c.Y - c.Radius) + placed.Max(c => c.Y + c.Radius)) / 2;
        var centered = placed.Select(c => c with { X = c.X - midX, Y = c.Y - midY }).ToList();
        return (centered, centered.Max(c => c.Distance(0, 0) + c.Radius));
    }
}
