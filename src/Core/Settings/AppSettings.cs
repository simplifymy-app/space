using System.Text.Json;
using SimplifyMySpace.Core.Localization;
using SimplifyMySpace.Core.Theme;

namespace SimplifyMySpace.Core.Settings;

public enum SizeKind
{
    OnDisk,
    Logical
}

public enum SizeUnits
{
    Decimal,
    Binary
}

public enum RemoveMode
{
    Trash,
    Permanent
}

public class AppSettings
{
    public Language Language { get; set; } = Language.System;

    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    public AccentPreset Accent { get; set; } = AccentPreset.Indigo;

    public ContrastLevel Contrast { get; set; } = ContrastLevel.Normal;

    public SizeKind SizeKind { get; set; } = SizeKind.OnDisk;

    public SizeUnits Units { get; set; } = SizeUnits.Decimal;

    public bool ShowHidden { get; set; } = true;

    public bool FollowSymlinks { get; set; }

    public bool ScanAnimation { get; set; } = true;

    public bool AutoUpdate { get; set; } = true;

    public RemoveMode RemoveMode { get; set; } = RemoveMode.Trash;

    public string LastLocation { get; set; } = string.Empty;
}

public class SettingsStore(string path)
{
    public SettingsStore()
        : this(AppData.Path("settings.json"))
    {
    }

    public AppSettings Current { get; } = Load(path);

    public event Action? Changed;

    public void Update(Action<AppSettings> change)
    {
        change(Current);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(Current, Json.Default.AppSettings));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error("Settings could not be written", exception);
        }

        Changed?.Invoke();
    }

    private static AppSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(path), Json.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            Log.Error("Settings could not be read", exception);
        }

        return new AppSettings();
    }
}
