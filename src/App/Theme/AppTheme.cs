using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Settings;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.App.Theme;

public static class AppTheme
{
    private const string FontSource = "avares://SimplifyMySpace/Assets/Fonts#";

    public static readonly FontFamily Latin = new(FontSource + Typography.Interface);

    public static readonly FontFamily Hand = new(FontSource + Typography.Hand);

    private static readonly FontFamily Cyrillic = new(FontSource + Typography.InterfaceCyrillic);

    private static SettingsStore? _store;

    public static readonly FontFamily Mono = new(FontSource + Typography.Mono);

    public static FontFamily Sans => Strings.Current == Language.Ukrainian ? Cyrillic : Latin;

    public static Palette Colors { get; private set; } = Palette.Dark;

    public static event Action? Changed;

    public static void Attach(SettingsStore store)
    {
        _store = store;
        Strings.Use(store.Current.Language);
        store.Changed += () =>
        {
            Strings.Use(store.Current.Language);
            Apply();
        };

        if (Application.Current?.PlatformSettings is { } platform)
        {
            platform.ColorValuesChanged += (_, _) => Apply();
        }

        Apply();
    }

    public static Color Color(Rgb rgb, double opacity = 1) =>
        Avalonia.Media.Color.FromArgb((byte)Math.Round(opacity * 255), rgb.R, rgb.G, rgb.B);

    public static IBrush Brush(Rgb rgb, double opacity = 1) => new SolidColorBrush(Color(rgb, opacity));

    private static void Apply()
    {
        if (_store is not { } store || Application.Current is not { } application)
        {
            return;
        }

        var settings = store.Current;
        var mode = settings.ThemeMode == ThemeMode.System ? SystemMode() : settings.ThemeMode;
        Colors = Palette.Resolve(mode, settings.Accent, settings.Contrast);
        application.RequestedThemeVariant = Colors.IsLight ? ThemeVariant.Light : ThemeVariant.Dark;

        var resources = application.Resources;
        resources["FontSans"] = Sans;
        resources["FontMono"] = Mono;
        foreach (var (key, rgb) in Roles(Colors))
        {
            resources[key] = Brush(rgb);
        }

        resources["AccentHover"] = Brush(Colors.Accent, States.Hover);
        resources["AccentPress"] = Brush(Colors.Accent, States.Press);
        resources["AccentSelected"] = Brush(Colors.Accent, States.Selected);
        Changed?.Invoke();
    }

    private static IEnumerable<(string, Rgb)> Roles(Palette palette) =>
    [
        ("Background", palette.Background),
        ("Surface", palette.Surface),
        ("SurfaceAlt", palette.SurfaceAlt),
        ("Hairline", palette.Hairline),
        ("Text", palette.Text),
        ("TextMuted", palette.TextMuted),
        ("TextFaint", palette.TextFaint),
        ("Accent", palette.Accent),
        ("OnAccent", palette.OnAccent),
        ("Danger", palette.Danger)
    ];

    private static ThemeMode SystemMode() =>
        Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Light
            ? ThemeMode.Light
            : ThemeMode.Dark;
}
