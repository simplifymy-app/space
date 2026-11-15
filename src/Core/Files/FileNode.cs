using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Files;

public sealed class FileNode(string name, FileNode? parent, bool isDirectory, DateTime modified, bool isHidden)
{
    private long _onDisk;
    private long _logical;
    private long _items;
    private volatile FileNode[] _children = [];

    public string Name { get; } = name;

    public FileNode? Parent { get; private set; } = parent;

    public bool IsDirectory { get; } = isDirectory;

    public bool IsHidden { get; } = isHidden;

    public DateTime Modified { get; } = modified;

    public bool Unreadable { get; internal set; }

    public long OnDisk => Interlocked.Read(ref _onDisk);

    public long Logical => Interlocked.Read(ref _logical);

    public long Items => Interlocked.Read(ref _items);

    public IReadOnlyList<FileNode> Children => _children;

    public string Path => Parent is null ? Name : System.IO.Path.Combine(Parent.Path, Name);

    public string Extension => IsDirectory ? string.Empty : System.IO.Path.GetExtension(Name).TrimStart('.');

    public long Size(SizeKind kind) => kind == SizeKind.OnDisk ? OnDisk : Logical;

    public bool IsWithin(FileNode ancestor)
    {
        for (var node = this; node is not null; node = node.Parent)
        {
            if (node == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<FileNode> Ancestry()
    {
        var chain = new List<FileNode>();
        for (var node = this; node is not null; node = node.Parent)
        {
            chain.Add(node);
        }

        chain.Reverse();
        return chain;
    }

    public void Detach()
    {
        if (Parent is not { } parent)
        {
            return;
        }

        parent._children = parent._children.Where(child => child != this).ToArray();
        parent.Grow(-OnDisk, -Logical, -(Items + 1));
        Parent = null;
    }

    internal void Adopt(FileNode[] children) => _children = children;

    internal void Measure(long onDisk, long logical)
    {
        _onDisk = onDisk;
        _logical = logical;
    }

    internal void Grow(long onDisk, long logical, long items)
    {
        for (var node = this; node is not null; node = node.Parent)
        {
            Interlocked.Add(ref node._onDisk, onDisk);
            Interlocked.Add(ref node._logical, logical);
            Interlocked.Add(ref node._items, items);
        }
    }
}
