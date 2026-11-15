using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Map;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Text;

namespace SimplifyMySpace.Core.Tests;

public class RulesTests
{
    [TestCase(0, SizeUnits.Decimal, "0 B")]
    [TestCase(999, SizeUnits.Decimal, "999 B")]
    [TestCase(1_500_000, SizeUnits.Decimal, "1.5 MB")]
    [TestCase(223_840_000_000, SizeUnits.Decimal, "223.8 GB")]
    [TestCase(7_200_000, SizeUnits.Decimal, "7.2 MB")]
    [TestCase(1024, SizeUnits.Binary, "1 KiB")]
    [TestCase(-2_000_000_000, SizeUnits.Decimal, "-2 GB")]
    public void FormatsBytes(long bytes, SizeUnits units, string expected) =>
        Assert.That(Format.Bytes(bytes, units), Is.EqualTo(expected));

    [Test]
    public void MiddleTruncateKeepsBothEnds()
    {
        var text = Format.MiddleTruncate("/Users/me/Library/Application Support/Microsoft/abcdef", 20);
        Assert.That(text, Has.Length.EqualTo(20));
        Assert.That(text, Does.StartWith("/Users/me"));
        Assert.That(text, Does.EndWith("abcdef"));
    }

    [Test]
    public void ProtectsSystemFoldersAndAncestorsOfKeptPaths()
    {
        var root = Path.GetTempPath();
        var system = Path.Combine(root, "sys");
        var home = Path.Combine(root, "users", "me");
        var protection = new Protection([system], [home]);

        Assert.That(protection.IsProtected(Path.Combine(system, "lib")), Is.True);
        Assert.That(protection.IsProtected(home), Is.True);
        Assert.That(protection.IsProtected(Path.Combine(root, "users")), Is.True);
        Assert.That(protection.IsProtected(Path.Combine(home, "Downloads")), Is.False);
        Assert.That(protection.IsProtected(Path.Combine(root, "system-other")), Is.False);
        Assert.That(protection.IsProtected(Path.GetPathRoot(root)!), Is.True);
    }

    [Test]
    public void WithinDoesNotMatchSiblingPrefixes()
    {
        Assert.That(ScanRules.IsWithin("/data/app", "/data"), Is.True);
        Assert.That(ScanRules.IsWithin("/database", "/data"), Is.False);
        Assert.That(ScanRules.IsWithin("/data", "/data/"), Is.True);
    }

    [Test]
    public void PackedCirclesDoNotOverlapAndStayInside()
    {
        var radii = Enumerable.Range(1, 30).Select(index => 1.0 / index).ToList();
        var (circles, radius) = CirclePacking.Pack(radii, 0.01);

        for (var i = 0; i < circles.Count; i++)
        {
            Assert.That(circles[i].Distance(0, 0) + circles[i].Radius, Is.LessThanOrEqualTo(radius + 1e-9));
            for (var j = i + 1; j < circles.Count; j++)
            {
                Assert.That(circles[i].Overlaps(circles[j], 0), Is.False, $"{i} overlaps {j}");
            }
        }
    }

    [Test]
    public async Task MapPreviewsGrandchildrenOnlyInsideLargeBubbles()
    {
        using var tree = new TempTree();
        tree.File("big/one.bin", 60_000);
        tree.File("big/two.bin", 30_000);
        tree.File("tiny.bin", 10);
        var scanner = new Scanner(tree.Root, new PortableFileSystem(), false);
        await scanner.Run(CancellationToken.None);

        var layout = MapLayout.Build(scanner.Root, SizeKind.Logical, true, 800, 800);
        var big = layout.Bubbles.Single(bubble => bubble.Node?.Name == "big");

        Assert.That(big.Inner.Select(inner => inner.Node!.Name), Is.EquivalentTo(new[] { "one.bin", "two.bin" }));
        foreach (var inner in big.Inner)
        {
            Assert.That(inner.Circle.Distance(big.Circle.X, big.Circle.Y) + inner.Circle.Radius,
                Is.LessThanOrEqualTo(big.Circle.Radius));
        }

        Assert.That(layout.Bubbles.Where(bubble => bubble.Node is null).Sum(bubble => bubble.Size), Is.EqualTo(10));
    }

    [Test]
    public void EveryKeyIsTranslatedWithMatchingPlaceholders()
    {
        var english = Strings.Table(Language.English);
        var ukrainian = Strings.Table(Language.Ukrainian);

        Assert.That(ukrainian.Keys, Is.EquivalentTo(english.Keys));
        foreach (var (key, text) in english)
        {
            Assert.That(Placeholders(ukrainian[key]), Is.EqualTo(Placeholders(text)), key);
        }
    }

    private static string[] Placeholders(string text) =>
        System.Text.RegularExpressions.Regex.Matches(text, @"\{\d\}").Select(match => match.Value).Order().ToArray();
}
