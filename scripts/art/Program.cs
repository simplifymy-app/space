using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using SimplifyMySpace.App;
using SimplifyMySpace.App.Theme;
using SimplifyMySpace.App.Views;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Theme;

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
using var session = HeadlessUnitTestSession.StartNew(typeof(Entry));
await session.Dispatch(() =>
{
    var store = new SettingsStore(Path.Combine(Path.GetTempPath(), "space-art-settings.json"));
    store.Current.Language = Language.English;
    store.Current.Accent = AccentPreset.Indigo;
    store.Current.ThemeMode = ThemeMode.Dark;
    AppTheme.Attach(store);

    Save(Banner(600, 400, 1.9, 44, 0.56), "assets/installer/splash.png");
    Save(Banner(1280, 280, 2.3, 52, 0.36), "assets/brand/banner.png");
    Save(Logo(), "assets/brand/logo.png");
    store.Update(settings => settings.ThemeMode = ThemeMode.Light);
    Save(Logo(), "assets/brand/logo-light.png");
    store.Update(settings => settings.ThemeMode = ThemeMode.Dark);
    Save(MsiBanner(), "build/art/msi-banner.png");
    Save(MsiLogo(), "build/art/msi-logo.png");

    store.Update(settings => settings.ThemeMode = ThemeMode.Light);
    Icons();

    store.Update(settings =>
    {
        settings.ThemeMode = ThemeMode.Dark;
        settings.SizeKind = SizeKind.Logical;
        settings.LastLocation = string.Empty;
    });
    Screenshots(new Services(store));
}, CancellationToken.None);

void Save(Control art, string path)
{
    var window = new Window { Content = art, SystemDecorations = SystemDecorations.None };
    window.Show();
    Dispatcher.UIThread.RunJobs();
    var size = new PixelSize((int)art.Width, (int)art.Height);
    using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96, 96));
    bitmap.Render(art);
    var target = Path.Combine(root, path);
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    bitmap.Save(target);
    window.Close();
    Console.WriteLine(path);
}

void Screenshots(Services services)
{
    var home = DemoHome();
    try
    {
        var window = new MainWindow(services) { Width = 1040, Height = 680 };
        window.Show();
        Capture(window, "assets/screenshots/start.png");
        window.Scan(home);
        for (var tick = 0; tick < 30; tick++)
        {
            Thread.Sleep(100);
            services.Store.Update(_ => { });
            Dispatcher.UIThread.RunJobs();
        }

        Capture(window, "assets/screenshots/results.png");
        window.OpenSettings();
        Capture(window, "assets/screenshots/settings.png");
        window.Close();
    }
    finally
    {
        Directory.Delete(home, true);
    }
}

void Capture(Window window, string path)
{
    Dispatcher.UIThread.RunJobs();
    var target = Path.Combine(root, path);
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    window.CaptureRenderedFrame()?.Save(target);
    Console.WriteLine(path);
}

string DemoHome()
{
    var home = Path.Combine("/tmp", "Home");
    if (Directory.Exists(home))
    {
        Directory.Delete(home, true);
    }

    const long gigabyte = 1_000_000_000;
    (string Path, double Gigabytes)[] files =
    [
        ("Pictures/Photos Library.photoslibrary/originals/2024.heic", 21.6),
        ("Pictures/Photos Library.photoslibrary/originals/2025.heic", 11.2),
        ("Pictures/Exports/wedding.zip", 3.4),
        ("Movies/Trip to Lisbon.mov", 12.4),
        ("Movies/Drone/clip-01.mp4", 4.1),
        ("Movies/Drone/clip-02.mp4", 3.7),
        ("Movies/Screen Recordings/demo.mov", 1.8),
        ("Library/Developer/Xcode/DerivedData/App/build.db", 14.9),
        ("Library/Developer/CoreSimulator/Devices/runtime.img", 9.6),
        ("Library/Caches/com.spotify.client/data", 4.2),
        ("Library/Caches/Google/Chrome/cache", 2.6),
        ("Library/Containers/com.docker.docker/Docker.raw", 18.3),
        ("Library/Application Support/MobileSync/Backup/iPhone.backup", 26.5),
        ("Downloads/Xcode_16.xip", 7.9),
        ("Downloads/ubuntu-24.04-desktop.iso", 6.1),
        ("Downloads/Figma.dmg", 0.42),
        ("Downloads/invoices.zip", 0.18),
        ("Music/Logic/Projects/album.logicx", 8.3),
        ("Music/Music/Media.localized/library.musiclibrary", 5.2),
        ("Documents/Contracts/2025.pdf", 0.09),
        ("Documents/Taxes.numbers", 0.02),
        ("Projects/web/node_modules/cache.bin", 2.9),
        ("Projects/mobile/build/app.apk", 1.1),
        ("Desktop/Screenshot 2025-11-12.png", 0.004)
    ];

    foreach (var (relative, gigabytes) in files)
    {
        var path = Path.Combine(home, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        stream.SetLength((long)(gigabytes * gigabyte));
    }

    return home;
}

void Icons()
{
    AppMark.Bitmap(256).Save(Path.Combine(root, "assets/icon/icon-windows-256.png"));
    Padded(1024).Save(Path.Combine(root, "assets/icon/icon-mac-1024.png"));

    var iconset = Path.Combine(root, "build/art/AppIcon.iconset");
    Directory.CreateDirectory(iconset);
    foreach (var size in new[] { 16, 32, 128, 256, 512 })
    {
        Padded(size).Save(Path.Combine(iconset, $"icon_{size}x{size}.png"));
        Padded(size * 2).Save(Path.Combine(iconset, $"icon_{size}x{size}@2x.png"));
    }

    var sizes = new[] { 16, 24, 32, 48, 64, 256 };
    var images = sizes.Select(size =>
    {
        using var stream = new MemoryStream();
        AppMark.Bitmap(size).Save(stream);
        return stream.ToArray();
    }).ToList();
    using var ico = new BinaryWriter(File.Create(Path.Combine(root, "assets/icon/AppIcon.ico")));
    ico.Write((ushort)0);
    ico.Write((ushort)1);
    ico.Write((ushort)sizes.Length);
    var offset = 6 + 16 * sizes.Length;
    for (var index = 0; index < sizes.Length; index++)
    {
        ico.Write((byte)(sizes[index] % 256));
        ico.Write((byte)(sizes[index] % 256));
        ico.Write((byte)0);
        ico.Write((byte)0);
        ico.Write((ushort)1);
        ico.Write((ushort)32);
        ico.Write(images[index].Length);
        ico.Write(offset);
        offset += images[index].Length;
    }

    images.ForEach(ico.Write);
    Console.WriteLine("assets/icon/AppIcon.ico, icon PNGs, build/art/AppIcon.iconset");
}

Avalonia.Media.Imaging.RenderTargetBitmap Padded(int pixels)
{
    var inset = pixels * 100 / 1024.0;
    var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(pixels, pixels), new Vector(96, 96));
    using (var context = bitmap.CreateDrawingContext())
    {
        using var mark = AppMark.Bitmap(Math.Max(1, (int)Math.Round(pixels - inset * 2)));
        context.DrawImage(mark, new Rect(inset, inset, pixels - inset * 2, pixels - inset * 2));
    }

    return bitmap;
}

Control Fade(Rgb color, double from, double to) => new Border
{
    Background = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(AppTheme.Color(color, 0), from),
            new GradientStop(AppTheme.Color(color), to)
        }
    }
};

Control Frame(double width, double height, Control content, IBrush? background) => new Border
{
    Width = width,
    Height = height,
    Background = background,
    Child = content
};

IBrush Paint(Rgb rgb) => AppTheme.Brush(rgb);

Control Banner(double width, double height, double scale, double left, double start)
{
    var tagline = Ui.Text(Strings.Get("settings.tagline"), Typography.Section, Ink.Text);
    tagline.FontSize = Typography.Section.Size * scale * 0.7;
    tagline.LetterSpacing = Typography.Section.Tracking * tagline.FontSize;
    tagline.Margin = new Thickness(0, 14 * scale, 0, 0);
    var stack = new StackPanel
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(left, 0, 0, 0),
        Children = { new BrandLogo(scale), tagline }
    };
    return Frame(width, height, new Panel { Children = { new FileTiles(start), stack } }, Paint(AppTheme.Colors.Background));
}

Control Logo()
{
    var logo = new BrandLogo(4)
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
    return Frame(560, 200, logo, null);
}

Control MsiBanner()
{
    var panel = new Panel
    {
        Width = 156,
        HorizontalAlignment = HorizontalAlignment.Left,
        Background = Paint(AppTheme.Colors.Background),
        ClipToBounds = true,
        Children =
        {
            new FileTiles(0),
            Fade(AppTheme.Colors.Background, 0.25, 0.62),
            new BrandLogo(0.95)
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(14, 0, 0, 40)
            }
        }
    };
    return Frame(493, 312, panel, Brushes.White);
}

Control MsiLogo()
{
    var strip = new FileTiles(0.15) { Width = 190, HorizontalAlignment = HorizontalAlignment.Right };
    var holder = new Border { Background = Paint(AppTheme.Colors.Background), Child = strip, Width = 190, HorizontalAlignment = HorizontalAlignment.Right };
    return Frame(493, 58, new Panel { Children = { holder } }, Brushes.White);
}

public class Entry
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
