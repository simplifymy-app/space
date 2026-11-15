using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.Core.Files;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Views;

public sealed class MainWindow : Window
{
    private readonly Services _services;
    private readonly ContentControl _host = new();
    private readonly ContentControl _settings = new() { IsVisible = false };
    private readonly Border _toast;
    private readonly TextBlock _toastText = Ui.Text(string.Empty, Typography.Value);
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private Func<Control> _build;

    public MainWindow(Services services)
    {
        _services = services;
        _build = () => new StartView(this, services);
        Title = Strings.Get("app.name");
        Width = 1040;
        Height = 680;
        MinWidth = 860;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _toast = new Border
        {
            Padding = new Thickness(16, 10),
            CornerRadius = new CornerRadius(Radius.Control),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, Sizes.BottomBar + Space.RowGap),
            Child = _toastText,
            Opacity = 0,
            IsHitTestVisible = false,
            Transitions =
            [
                new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(Motion.Enter) }
            ]
        };
        Ui.Paint(_toast, Border.BackgroundProperty, Ink.SurfaceAlt);
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toast.Opacity = 0;
        };

        Content = new Panel { Children = { _host, _settings, _toast } };
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, Dropped);
        Strings.Changed += Rebuild;
        AppTheme.Changed += Repaint;
        Closed += (_, _) =>
        {
            Strings.Changed -= Rebuild;
            AppTheme.Changed -= Repaint;
        };
        Repaint();
        Rebuild();
    }

    public void ShowStart()
    {
        _build = () => new StartView(this, _services);
        Rebuild();
    }

    public void Scan(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        _services.Store.Update(settings => settings.LastLocation = path);
        var stop = new CancellationTokenSource();
        var scanner = new Scanner(path, _services.FileSystem, _services.Settings.FollowSymlinks);
        var session = new Session(scanner, Protection.ForThisMachine.IsProtected);
        var explore = new Explore(session, _services, scanner.Run(stop.Token), stop);
        _build = () => new ExploreView(this, explore);
        Rebuild();
    }

    public void Toast(string text, bool linger = true)
    {
        _toastText.Text = text;
        _toast.Opacity = 1;
        _toastTimer.Stop();
        if (linger)
        {
            _toastTimer.Start();
        }
    }

    public void OpenSettings()
    {
        var view = new SettingsView(_services, CloseSettings);
        _settings.Content = view;
        _settings.IsVisible = true;
        _host.IsVisible = false;
        view.Focus();
    }

    private void CloseSettings()
    {
        _settings.IsVisible = false;
        _settings.Content = null;
        _host.IsVisible = true;
    }

    private void Repaint() => Icon = AppMark.WindowIcon();

    private void Rebuild()
    {
        Title = Strings.Get("app.name");
        _host.Content = _build();
    }

    private void Dropped(object? sender, DragEventArgs e)
    {
        var folder = e.DataTransfer.TryGetFiles()?.OfType<IStorageFolder>().FirstOrDefault()?.TryGetLocalPath();
        if (folder is not null)
        {
            Scan(folder);
        }
    }
}
