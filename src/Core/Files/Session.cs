using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Files;

public sealed class Basket(Func<string, bool> isProtected)
{
    private readonly List<FileNode> _items = [];

    public IReadOnlyList<FileNode> Items => _items;

    public event Action? Changed;

    public bool IsMarked(FileNode node) => _items.Contains(node);

    public bool IsCovered(FileNode node) => _items.Any(node.IsWithin);

    public bool CanMark(FileNode node) => node.Parent is not null && !isProtected(node.Path);

    public long Total(SizeKind kind) => _items.Sum(item => item.Size(kind));

    public long MarkedInside(FileNode folder, SizeKind kind) =>
        _items.Where(item => item != folder && item.IsWithin(folder)).Sum(item => item.Size(kind));

    public void Toggle(FileNode node)
    {
        if (_items.Remove(node))
        {
            Changed?.Invoke();
            return;
        }

        if (IsCovered(node) || !CanMark(node))
        {
            return;
        }

        _items.RemoveAll(item => item.IsWithin(node));
        _items.Add(node);
        Changed?.Invoke();
    }

    public void Remove(FileNode node)
    {
        if (_items.RemoveAll(item => item.IsWithin(node)) > 0)
        {
            Changed?.Invoke();
        }
    }
}

public sealed class Navigator(FileNode root)
{
    private readonly Stack<FileNode> _back = new();
    private readonly Stack<FileNode> _forward = new();

    public FileNode Root { get; } = root;

    public FileNode Current { get; private set; } = root;

    public bool CanGoBack => _back.Count > 0;

    public bool CanGoForward => _forward.Count > 0;

    public bool CanGoUp => Current != Root;

    public event Action? Changed;

    public void Open(FileNode folder)
    {
        if (!folder.IsDirectory || folder == Current || !folder.IsWithin(Root))
        {
            return;
        }

        _back.Push(Current);
        _forward.Clear();
        Move(folder);
    }

    public void Up()
    {
        if (CanGoUp && Current.Parent is { } parent)
        {
            Open(parent);
        }
    }

    public void Back()
    {
        if (_back.TryPop(out var previous))
        {
            _forward.Push(Current);
            Move(previous);
        }
    }

    public void Forward()
    {
        if (_forward.TryPop(out var next))
        {
            _back.Push(Current);
            Move(next);
        }
    }

    public void Forget(FileNode removed, FileNode formerParent)
    {
        Prune(_back, removed);
        Prune(_forward, removed);
        if (Current.IsWithin(removed))
        {
            while (_back.TryPeek(out var top) && top == formerParent)
            {
                _back.Pop();
            }

            Move(formerParent);
        }
    }

    private static void Prune(Stack<FileNode> stack, FileNode removed)
    {
        var kept = stack.Reverse().Where(node => node != removed && !node.IsWithin(removed)).ToList();
        stack.Clear();
        kept.ForEach(stack.Push);
    }

    private void Move(FileNode folder)
    {
        Current = folder;
        Changed?.Invoke();
    }
}

public sealed class Session(Scanner scanner, Func<string, bool> isProtected)
{
    public Scanner Scanner { get; } = scanner;

    public FileNode Root => Scanner.Root;

    public Navigator Navigator { get; } = new(scanner.Root);

    public Basket Basket { get; } = new(isProtected);

    public FileNode? Selected { get; private set; }

    public FileNode? Hovered { get; private set; }

    public event Action? SelectionChanged;

    public event Action? HoverChanged;

    public void Select(FileNode? node)
    {
        if (Selected == node)
        {
            return;
        }

        Selected = node;
        SelectionChanged?.Invoke();
    }

    public void Hover(FileNode? node)
    {
        if (Hovered == node)
        {
            return;
        }

        Hovered = node;
        HoverChanged?.Invoke();
    }

    public void Forget(IEnumerable<FileNode> removed)
    {
        foreach (var node in removed)
        {
            var parent = node.Parent ?? Root;
            node.Detach();
            Basket.Remove(node);
            Navigator.Forget(node, parent);
            if (Selected is { } selected && (selected == node || selected.IsWithin(node)))
            {
                Select(null);
            }
        }
    }
}
