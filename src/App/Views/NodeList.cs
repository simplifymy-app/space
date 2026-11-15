using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SimplifyMySpace.Core.Files;

namespace SimplifyMySpace.App.Views;

public sealed class NodeList : Border
{
    private readonly Explore _explore;
    private readonly ListBox _list;
    private readonly HashSet<NodeRow> _rows = [];
    private FileNode? _folder;
    private List<FileNode> _items = [];

    public NodeList(Explore explore)
    {
        _explore = explore;
        _list = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new FuncDataTemplate<FileNode>((_, _) => new NodeRow(explore, this), true)
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        _list.SelectionChanged += (_, _) =>
        {
            if (_list.SelectedItem is FileNode node)
            {
                explore.Session.Select(node);
            }
        };
        _list.DoubleTapped += (_, _) =>
        {
            if (_list.SelectedItem is FileNode node)
            {
                explore.Activate(node);
            }
        };
        Child = _list;
    }

    public void Show(FileNode folder)
    {
        _folder = folder;
        _items = Sorted();
        _list.ItemsSource = _items;
        _list.Scroll?.Offset = default;
    }

    public void Refresh()
    {
        var next = Sorted();
        if (!next.SequenceEqual(_items))
        {
            var selected = _list.SelectedItem;
            _items = next;
            _list.ItemsSource = _items;
            _list.SelectedItem = selected;
        }

        foreach (var row in _rows)
        {
            row.Update();
        }
    }

    public void Repaint()
    {
        foreach (var row in _rows)
        {
            row.Paint();
        }
    }

    public void Reveal(FileNode? node)
    {
        if (_list.SelectedItem == node)
        {
            return;
        }

        _list.SelectedItem = node is not null && _items.Contains(node) ? node : null;
        if (_list.SelectedItem is not null)
        {
            _list.ScrollIntoView(_list.SelectedItem);
        }
    }

    public void FocusList() => _list.Focus();

    internal void Track(NodeRow row, bool live)
    {
        if (live)
        {
            _rows.Add(row);
        }
        else
        {
            _rows.Remove(row);
        }
    }

    private List<FileNode> Sorted()
    {
        if (_folder is null)
        {
            return [];
        }

        var kind = _explore.Settings.SizeKind;
        return _folder.Children
            .Where(child => _explore.Settings.ShowHidden || !child.IsHidden)
            .OrderByDescending(child => child.Size(kind))
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
