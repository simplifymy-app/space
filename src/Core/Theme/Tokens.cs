namespace SimplifyMySpace.Core.Theme;

public sealed record TypeRole(double Size, int Weight, double Tracking, bool Mono, bool Uppercase = false);

public static class Typography
{
    public const string Interface = "Space Grotesk";

    public const string InterfaceCyrillic = "Manrope";

    public const string Mono = "IBM Plex Mono";

    public const string Hand = "Caveat";

    public static readonly TypeRole Display = new(27, 600, -0.03, false);

    public static readonly TypeRole Headline = new(22, 600, -0.02, false);

    public static readonly TypeRole Title = new(19, 600, -0.02, false);

    public static readonly TypeRole Body = new(14.5, 400, 0, false);

    public static readonly TypeRole Label = new(14, 500, 0, false);

    public static readonly TypeRole Value = new(13, 400, 0, true);

    public static readonly TypeRole Meta = new(11.5, 400, 0, true);

    public static readonly TypeRole Section = new(10.5, 500, 0.09, true, true);
}

public static class Space
{
    public const double Gutter = 4;

    public const double Base = 8;

    public const double Inset = 12;

    public const double RowGap = 16;

    public const double Screen = 20;

    public const double Section = 22;

    public const double HitTarget = 44;

    public const double Row = 56;

    public const double SettingsRow = 48;
}

public static class Radius
{
    public const double Control = 4;

    public const double Panel = 10;

    public const double Sheet = 14;
}

public static class Sizes
{
    public const double Hairline = 1;

    public const double Button = 34;

    public const double Icon = 20;

    public const double Tile = 34;

    public const double TopBar = 52;

    public const double BottomBar = 56;

    public const double Bar = 4;

    public const double ToggleWidth = 44;

    public const double ToggleHeight = 26;

    public const double ToggleKnob = 20;
}

public static class States
{
    public const double Hover = 0.06;

    public const double Press = 0.12;

    public const double Selected = 0.16;

    public const double Disabled = 0.38;

    public const double Scrim = 0.64;

    public const double FocusRing = 2;
}

public static class Motion
{
    public const int State = 120;

    public const int Theme = 160;

    public const int Enter = 240;

    public const int Exit = 320;
}
