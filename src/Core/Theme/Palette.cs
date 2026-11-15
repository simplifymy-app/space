namespace SimplifyMySpace.Core.Theme;

public enum ThemeMode
{
    System,
    Dark,
    Black,
    Light
}

public enum AccentPreset
{
    Indigo,
    Amber,
    Jade,
    Rose,
    Violet
}

public enum ContrastLevel
{
    Soft,
    Normal,
    High
}

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Hex(uint value) => new((byte)(value >> 16), (byte)(value >> 8), (byte)value);

    public Rgb Mix(Rgb to, double amount) => new(
        (byte)(R + (to.R - R) * amount),
        (byte)(G + (to.G - G) * amount),
        (byte)(B + (to.B - B) * amount));
}

public sealed record Palette(
    Rgb Background,
    Rgb Surface,
    Rgb SurfaceAlt,
    Rgb Hairline,
    Rgb Text,
    Rgb TextMuted,
    Rgb TextFaint,
    Rgb Accent,
    Rgb OnAccent,
    Rgb Danger,
    bool IsLight)
{
    private static readonly Rgb DarkInk = Rgb.Hex(0x0B0B0C);

    private static readonly Rgb LightInk = Rgb.Hex(0xFFFFFF);

    public static readonly Palette Dark = new(
        Rgb.Hex(0x0B0B0C),
        Rgb.Hex(0x141416),
        Rgb.Hex(0x1C1C1F),
        Rgb.Hex(0x2A2A2E),
        Rgb.Hex(0xEDEDEF),
        Rgb.Hex(0x8A8A92),
        Rgb.Hex(0x5A5A62),
        Accents.Fill(AccentPreset.Indigo, false),
        DarkInk,
        Rgb.Hex(0xFF7A7A),
        false);

    public static readonly Palette Black = Dark with
    {
        Background = Rgb.Hex(0x000000),
        Surface = Rgb.Hex(0x0C0C0D),
        SurfaceAlt = Rgb.Hex(0x161618),
        Hairline = Rgb.Hex(0x232326)
    };

    public static readonly Palette Light = new(
        Rgb.Hex(0xFFFFFF),
        Rgb.Hex(0xF5F5F6),
        Rgb.Hex(0xEDEDEF),
        Rgb.Hex(0xE3E3E6),
        Rgb.Hex(0x131315),
        Rgb.Hex(0x5E5E66),
        Rgb.Hex(0x74747C),
        Accents.Fill(AccentPreset.Indigo, true),
        LightInk,
        Rgb.Hex(0xC22B2B),
        true);

    public static Palette Resolve(ThemeMode mode, AccentPreset accent, ContrastLevel contrast)
    {
        var palette = mode switch
        {
            ThemeMode.Light => Light,
            ThemeMode.Black => Black,
            _ => Dark
        };

        var tinted = palette with
        {
            Accent = Accents.Fill(accent, palette.IsLight),
            OnAccent = palette.IsLight ? LightInk : DarkInk
        };

        var amount = contrast switch
        {
            ContrastLevel.Soft => -0.12,
            ContrastLevel.High => 0.16,
            _ => 0
        };

        return amount == 0
            ? tinted
            : tinted with
            {
                Text = tinted.Shift(tinted.Text, amount),
                TextMuted = tinted.Shift(tinted.TextMuted, amount),
                TextFaint = tinted.Shift(tinted.TextFaint, amount)
            };
    }

    private Rgb Shift(Rgb role, double amount) => amount < 0
        ? role.Mix(Background, -amount)
        : role.Mix(IsLight ? Rgb.Hex(0x000000) : Rgb.Hex(0xFFFFFF), amount);
}

public static class Accents
{
    public static Rgb Fill(AccentPreset preset, bool light) => preset switch
    {
        AccentPreset.Amber => Rgb.Hex(light ? 0x8F5A12u : 0xE0A458u),
        AccentPreset.Jade => Rgb.Hex(light ? 0x12764Eu : 0x5FBF8Fu),
        AccentPreset.Rose => Rgb.Hex(light ? 0xA62F55u : 0xD96A8Bu),
        AccentPreset.Violet => Rgb.Hex(light ? 0x6541D6u : 0xA98BFFu),
        _ => Rgb.Hex(light ? 0x4A5FE0u : 0x6E8BFFu)
    };
}
