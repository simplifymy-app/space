using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.App.Views;

namespace SimplifyMySpace.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new Services();
            AppTheme.Attach(services.Store);
            desktop.MainWindow = new MainWindow(services);
            Updates.CheckInBackground(services.Settings);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
