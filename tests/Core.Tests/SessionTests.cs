using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Tests;

public class SessionTests
{
    [Test]
    public async Task DetachSubtractsFromEveryAncestor()
    {
        using var tree = new TempTree();
        tree.File("a/b/big.bin", 5000);
        tree.File("a/small.bin", 100);
        var session = await Start(tree.Root);
        var a = Child(session.Root, "a");
        var b = Child(a, "b");

        session.Forget([b]);

        Assert.That(a.Logical, Is.EqualTo(100));
        Assert.That(session.Root.Logical, Is.EqualTo(100));
        Assert.That(a.Items, Is.EqualTo(1));
        Assert.That(a.Children.Select(child => child.Name), Is.EquivalentTo(new[] { "small.bin" }));
    }

    [Test]
    public async Task MarkingAFolderAbsorbsMarkedDescendants()
    {
        using var tree = new TempTree();
        tree.File("a/b/big.bin", 5000);
        var session = await Start(tree.Root);
        var a = Child(session.Root, "a");
        var b = Child(a, "b");

        session.Basket.Toggle(b);
        session.Basket.Toggle(a);
        session.Basket.Toggle(Child(b, "big.bin"));

        Assert.That(session.Basket.Items, Is.EquivalentTo(new[] { a }));
        Assert.That(session.Basket.IsCovered(b), Is.True);
        Assert.That(session.Basket.Total(SizeKind.Logical), Is.EqualTo(5000));
    }

    [Test]
    public async Task MarkedInsideSumsOnlyMarkedDescendants()
    {
        using var tree = new TempTree();
        tree.File("a/b/big.bin", 5000);
        tree.File("a/c/small.bin", 300);
        var session = await Start(tree.Root);
        var a = Child(session.Root, "a");

        session.Basket.Toggle(Child(a, "b"));

        Assert.That(session.Basket.MarkedInside(a, SizeKind.Logical), Is.EqualTo(5000));
        Assert.That(session.Basket.MarkedInside(Child(a, "b"), SizeKind.Logical), Is.Zero);
        Assert.That(session.Basket.MarkedInside(Child(a, "c"), SizeKind.Logical), Is.Zero);
    }

    [Test]
    public async Task ProtectedAndRootItemsCannotBeMarked()
    {
        using var tree = new TempTree();
        tree.File("keep/file.bin", 10);
        var session = await Start(tree.Root, path => path.EndsWith("keep", StringComparison.Ordinal));

        session.Basket.Toggle(session.Root);
        session.Basket.Toggle(Child(session.Root, "keep"));

        Assert.That(session.Basket.Items, Is.Empty);
    }

    [Test]
    public async Task NavigatorLeavesARemovedFolder()
    {
        using var tree = new TempTree();
        tree.File("a/b/big.bin", 10);
        var session = await Start(tree.Root);
        var a = Child(session.Root, "a");
        var b = Child(a, "b");
        session.Navigator.Open(a);
        session.Navigator.Open(b);

        session.Forget([b]);

        Assert.That(session.Navigator.Current, Is.EqualTo(a));
        session.Navigator.Back();
        Assert.That(session.Navigator.Current, Is.EqualTo(session.Root));
    }

    private static async Task<Session> Start(string path, Func<string, bool>? isProtected = null)
    {
        var scanner = new Scanner(path, new PortableFileSystem(), false);
        await scanner.Run(CancellationToken.None);
        return new Session(scanner, isProtected ?? (_ => false));
    }

    private static FileNode Child(FileNode parent, string name) => parent.Children.Single(child => child.Name == name);
}
