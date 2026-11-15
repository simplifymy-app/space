using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Text;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class ReviewWindow : Window
{
    private readonly Explore _explore;
    private readonly TextBlock _section = Ui.Section(string.Empty);
    private readonly TextBlock _title = Ui.Text(string.Empty, Typography.Headline);
    private readonly StackPanel _items = new() { Spacing = 2 };
    private readonly TextBlock _note = Ui.Text(string.Empty, Typography.Meta);
    private readonly Button _confirm = new();
    private RemoveMode _mode;

    public ReviewWindow(Explore explore)
    {
        _explore = explore;
        _mode = explore.Settings.RemoveMode;
        Title = Strings.Get("bottom.remove");
        Width = 560;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var cancel = Ui.Button("review.cancel", () => Close(null));
        _confirm.Click += (_, _) => Close(_mode);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.Base,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, _confirm }
        };

        var mode = new Segmented<RemoveMode>(Enum.GetValues<RemoveMode>(), _mode, Strings.Caption, chosen =>
        {
            _mode = chosen;
            Paint();
        }) { HorizontalAlignment = HorizontalAlignment.Left };

        Content = new StackPanel
        {
            Margin = new Thickness(Space.Section),
            Spacing = Space.Base,
            Children =
            {
                _section,
                _title,
                new ScrollViewer { MaxHeight = 320, Margin = new Thickness(0, Space.Base), Content = _items },
                mode,
                _note,
                Spaced(buttons)
            }
        };

        Build();
        Paint();
    }

    private void Build()
    {
        var basket = _explore.Session.Basket;
        if (basket.Items.Count == 0)
        {
            Close(null);
            return;
        }

        _section.Text = Strings.Get("review.section", Format.Count(basket.Items.Count)).ToUpperInvariant();
        _title.Text = Strings.Get("review.title", _explore.Bytes(basket.Total(_explore.Settings.SizeKind)));
        _items.Children.Clear();
        foreach (var item in basket.Items.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
        {
            _items.Children.Add(Row(item));
        }
    }

    private Control Row(FileNode item)
    {
        var text = new StackPanel
        {
            Spacing = 2,
            Children =
            {
                Ui.Text(item.Name, Typography.Label),
                Ui.Text(Format.MiddleTruncate(item.Parent?.Path ?? item.Path, 64), Typography.Meta, Ink.TextFaint)
            }
        };

        var unmark = Glyph.Button(Icons.Close, () =>
        {
            _explore.Session.Basket.Toggle(item);
            Build();
            Paint();
        });

        var row = new Grid
        {
            MinHeight = Space.HitTarget,
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = Space.Inset,
            Children = { text, Ui.Text(_explore.Size(item), Typography.Value, Ink.TextMuted), unmark }
        };
        Grid.SetColumn(row.Children[1], 1);
        Grid.SetColumn(unmark, 2);
        return row;
    }

    private void Paint()
    {
        var permanent = _mode == RemoveMode.Permanent;
        var total = _explore.Bytes(_explore.Session.Basket.Total(_explore.Settings.SizeKind));
        _confirm.Content = permanent
            ? Strings.Get("review.confirmDelete", total)
            : Strings.Get(OperatingSystem.IsWindows() ? "review.confirmRecycle" : "review.confirmTrash", total);
        _confirm.Classes.Set("primary", !permanent);
        _confirm.Classes.Set("danger", permanent);
        _note.Text = permanent ? Strings.Get("review.permanentNote") : Strings.ForPlatform("review.trashNote");
        Ui.Paint(_note, TextBlock.ForegroundProperty, permanent ? Ink.Danger : Ink.TextMuted);
    }

    private static Control Spaced(Control control)
    {
        control.Margin = new Thickness(0, Space.Base, 0, 0);
        return control;
    }
}
