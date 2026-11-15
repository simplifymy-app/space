using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class SettingsView : DockPanel
{
    private readonly Services _services;
    private readonly Action _close;
    private readonly ScrollViewer _body = new();

    public SettingsView(Services services, Action close)
    {
        _services = services;
        _close = close;
        Focusable = true;
        Ui.Paint(this, BackgroundProperty, Ink.Background);

        var bar = new Border
        {
            Height = Sizes.TopBar,
            Padding = new Thickness(Space.Inset, 0),
            BorderThickness = new Thickness(0, 0, 0, Sizes.Hairline)
        };
        Ui.Paint(bar, Border.BorderBrushProperty, Ink.Hairline);
        SetDock(bar, Dock.Top);
        Children.AddRange([bar, _body]);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                close();
                e.Handled = true;
            }
        };

        void Build()
        {
            bar.Child = BackButton();
            _body.Content = Page();
        }

        AttachedToVisualTree += (_, _) => Strings.Changed += Build;
        DetachedFromVisualTree += (_, _) => Strings.Changed -= Build;
        Build();
    }

    private Control BackButton()
    {
        var label = Ui.Text(Strings.Get("icon.back"), Typography.Label, Ink.TextMuted);
        label.FontSize = 13;
        var back = new Button
        {
            Classes = { "ghost" },
            Height = Glyph.IconButton,
            Padding = new Thickness(4, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children = { Glyph.Create(Icons.ChevronLeft, Ink.TextMuted), label }
            }
        };
        ToolTip.SetTip(back, "Esc");
        back.Click += (_, _) => _close();
        return back;
    }

    private Control Page()
    {
        var settings = _services.Settings;
        var version = typeof(SettingsView).Assembly.GetName().Version?.ToString(3) ?? string.Empty;
        return new StackPanel
        {
            MaxWidth = 640,
            Margin = new Thickness(Space.Section, Space.Screen, Space.Section, Space.Section),
            Children =
            {
                Ui.Text(Strings.Get("settings.title"), Typography.Display),
                new AboutBanner { Margin = new Thickness(0, Space.RowGap, 0, 0) },
                Section("settings.appearance"),
                Group(
                    Row("settings.theme", null, Choice(settings.ThemeMode, (s, v) => s.ThemeMode = v)),
                    Row("settings.animation", "settings.animationNote",
                        new Toggle(settings.ScanAnimation, v => Change(s => s.ScanAnimation = v))),
                    Row("settings.accent", null, new SwatchRow(settings.Accent, v => Change(s => s.Accent = v))),
                    Row("settings.contrast", null, Choice(settings.Contrast, (s, v) => s.Contrast = v)),
                    Row("settings.language", null, Choice(settings.Language, (s, v) => s.Language = v))),
                Section("settings.scanning"),
                Group(
                    Row("settings.sizes", "settings.sizesNote", Choice(settings.SizeKind, (s, v) => s.SizeKind = v)),
                    Row("settings.units", null, Choice(settings.Units, (s, v) => s.Units = v)),
                    Row("settings.hidden", "settings.hiddenNote",
                        new Toggle(settings.ShowHidden, v => Change(s => s.ShowHidden = v))),
                    Row("settings.links", "settings.linksNote",
                        new Toggle(settings.FollowSymlinks, v => Change(s => s.FollowSymlinks = v)))),
                Section("settings.removal"),
                Group(Row("settings.removeMode", null, Choice(settings.RemoveMode, (s, v) => s.RemoveMode = v))),
                Section("settings.about"),
                Group(
                    Row("settings.version", null, Ui.Text(version, Typography.Value, Ink.TextMuted)),
                    Row("settings.autoUpdate", null,
                        new Toggle(settings.AutoUpdate, v => Change(s => s.AutoUpdate = v))),
                    UpdateRow())
            }
        };
    }

    private void Change(Action<AppSettings> change) => _services.Store.Update(change);

    private Control UpdateRow()
    {
        var status = Ui.Text(string.Empty, Typography.Meta, Ink.TextMuted);
        var button = new Button { Height = 32, Classes = { "secondary" } };
        button.Click += (_, _) =>
        {
            if (Updates.State == UpdateState.Ready)
            {
                Updates.Restart();
            }
            else
            {
                Updates.Check();
            }
        };

        void Show()
        {
            status.Text = Updates.State == UpdateState.Ready
                ? Strings.Get("update.ready", Updates.ReadyVersion ?? string.Empty)
                : Strings.Caption(Updates.State);
            button.Content = Strings.Get(Updates.State == UpdateState.Ready ? "update.restart" : "update.check");
            button.IsEnabled = Updates.State != UpdateState.Checking;
        }

        Updates.Changed += Show;
        button.DetachedFromVisualTree += (_, _) => Updates.Changed -= Show;
        Show();
        var text = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { Ui.Text(Strings.Get("update.title"), Typography.Label), status }
        };
        var grid = new Grid
        {
            MinHeight = Space.SettingsRow,
            Margin = new Thickness(0, Space.Base),
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = Space.RowGap,
            Children = { text, button }
        };
        button.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(button, 1);
        return grid;
    }

    private Segmented<T> Choice<T>(T value, Action<AppSettings, T> apply) where T : struct, Enum =>
        new(Enum.GetValues<T>(), value, Strings.Caption, chosen => Change(settings => apply(settings, chosen)));

    private static TextBlock Section(string key)
    {
        var section = Ui.Section(Strings.Get(key));
        section.Margin = new Thickness(0, 28, 0, Space.Inset);
        return section;
    }

    private static Border Group(params Control[] rows)
    {
        var stack = new StackPanel();
        for (var index = 0; index < rows.Length; index++)
        {
            if (index > 0)
            {
                stack.Children.Add(Ui.Hairline());
            }

            stack.Children.Add(rows[index]);
        }

        var panel = new Border
        {
            CornerRadius = new CornerRadius(Radius.Panel),
            BorderThickness = new Thickness(Sizes.Hairline),
            Padding = new Thickness(Space.RowGap, 0),
            Child = stack
        };
        Ui.Paint(panel, Border.BackgroundProperty, Ink.Surface);
        Ui.Paint(panel, Border.BorderBrushProperty, Ink.Hairline);
        return panel;
    }

    private static Control Row(string key, string? noteKey, Control control)
    {
        var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(Ui.Text(Strings.Get(key), Typography.Label));
        if (noteKey is not null)
        {
            text.Children.Add(Ui.Text(Strings.Get(noteKey), Typography.Meta, Ink.TextMuted));
        }

        control.VerticalAlignment = VerticalAlignment.Center;
        var grid = new Grid
        {
            MinHeight = Space.SettingsRow,
            Margin = new Thickness(0, Space.Base),
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = Space.RowGap,
            Children = { text, control }
        };
        Grid.SetColumn(control, 1);
        return grid;
    }
}
