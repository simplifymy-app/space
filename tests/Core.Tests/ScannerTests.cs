using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Platform.Mac;

namespace SimplifyMySpace.Core.Tests;

public class ScannerTests
{
    [Test]
    public async Task CountsSizesAndItemsUpTheTree()
    {
        using var tree = new TempTree();
        tree.File("a/one.bin", 1000);
        tree.File("a/b/two.bin", 2000);
        tree.File("three.bin", 500);

        var root = await Scan(tree.Root, new PortableFileSystem());

        Assert.That(root.Logical, Is.EqualTo(3500));
        Assert.That(root.Items, Is.EqualTo(5));
        var a = root.Children.Single(child => child.Name == "a");
        Assert.That(a.Logical, Is.EqualTo(3000));
        Assert.That(a.Children.Single(child => child.Name == "b").Logical, Is.EqualTo(2000));
    }

    [Test]
    public async Task StoppedScanFinishesWithoutWaitingForTheTree()
    {
        using var tree = new TempTree();
        for (var index = 0; index < 200; index++)
        {
            tree.File($"d{index}/e/f.bin", 10);
        }

        var stopped = new CancellationTokenSource();
        await stopped.CancelAsync();
        var scanner = new Scanner(tree.Root, new PortableFileSystem(), false);

        var finished = await Task.WhenAny(scanner.Run(stopped.Token), Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.That(finished.IsCompletedSuccessfully, Is.True);
        Assert.That(scanner.Root.Items, Is.Zero);
    }

    [Test]
    public async Task UnreadableFolderDoesNotThrow()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Unix permissions only");
            return;
        }

        using var tree = new TempTree();
        var locked = tree.Folder("locked");
        File.SetUnixFileMode(locked, UnixFileMode.None);
        var thrown = 0;
        void Count(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) =>
            Interlocked.Increment(ref thrown);
        AppDomain.CurrentDomain.FirstChanceException += Count;
        try
        {
            var scanner = new Scanner(tree.Root, new SimplifyMySpace.Platform.Mac.MacFileSystem(), false);
            if (OperatingSystem.IsMacOS())
            {
                await scanner.Run(CancellationToken.None);
                Assert.That(scanner.Unreadable, Is.EqualTo(1));
                Assert.That(thrown, Is.Zero);
            }
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Count;
        }
    }

    [Test]
    public async Task DoesNotFollowFolderLinksByDefault()
    {
        using var tree = new TempTree();
        tree.File("real/data.bin", 4000);
        Directory.CreateSymbolicLink(Path.Combine(tree.Root, "link"), Path.Combine(tree.Root, "real"));

        var root = await Scan(tree.Root, new PortableFileSystem());

        Assert.That(root.Logical, Is.EqualTo(4000));
        Assert.That(root.Children.Select(child => child.Name), Is.EquivalentTo(new[] { "real" }));
    }

    [Test]
    public async Task FollowedLinksIntoTheScannedTreeAreNotCountedTwice()
    {
        using var tree = new TempTree();
        tree.File("real/data.bin", 4000);
        Directory.CreateSymbolicLink(Path.Combine(tree.Root, "real", "loop"), tree.Root);

        var root = await Scan(tree.Root, new PortableFileSystem(), followLinks: true);

        Assert.That(root.Logical, Is.EqualTo(4000));
    }

    [Test]
    public async Task UnreadableFolderIsSkippedAndCounted()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Unix permissions only");
            return;
        }

        using var tree = new TempTree();
        tree.File("open/data.bin", 100);
        var locked = tree.Folder("locked");
        tree.File("locked/secret.bin", 100);
        File.SetUnixFileMode(locked, UnixFileMode.None);

        var scanner = new Scanner(tree.Root, new PortableFileSystem(), false);
        await scanner.Run(CancellationToken.None);

        Assert.That(scanner.Unreadable, Is.EqualTo(1));
        Assert.That(scanner.Root.Children.Single(child => child.Name == "locked").Unreadable, Is.True);
        Assert.That(scanner.Root.Logical, Is.EqualTo(100));
    }

    [Test]
    public async Task HardLinkedFileCountsOnce()
    {
        Assume.That(OperatingSystem.IsMacOS(), Is.True);
        using var tree = new TempTree();
        var original = tree.File("a/original.bin", 64_000);
        tree.Folder("b");
        using (var link = System.Diagnostics.Process.Start("ln", [original, Path.Combine(tree.Root, "b", "copy.bin")]))
        {
            await link!.WaitForExitAsync();
        }

        var root = await Scan(tree.Root, new MacFileSystem());

        Assert.That(root.Logical, Is.EqualTo(64_000));
        Assert.That(root.Size(SizeKind.OnDisk), Is.GreaterThanOrEqualTo(64_000));
    }

    private static async Task<FileNode> Scan(string path, IFileSystem fileSystem, bool followLinks = false)
    {
        var scanner = new Scanner(path, fileSystem, followLinks);
        await scanner.Run(CancellationToken.None);
        return scanner.Root;
    }
}
