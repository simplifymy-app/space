using System.Globalization;
using System.Text.Json;

namespace SimplifyMySpace.Core.Localization;

public enum Language
{
    System,
    English,
    Ukrainian
}

public static class Strings
{
    private static readonly IReadOnlyDictionary<string, string> Base = Table(Language.English);

    private static IReadOnlyDictionary<string, string> _active = Base;

    public static Language Current { get; private set; } = Language.English;

    public static event Action? Changed;

    public static void Use(Language language)
    {
        var resolved = Resolve(language);
        if (resolved == Current)
        {
            return;
        }

        Current = resolved;
        _active = resolved == Language.English ? Base : Table(resolved);
        Changed?.Invoke();
    }

    public static string Get(string key) =>
        _active.TryGetValue(key, out var text) ? text : English(key);

    public static string Get(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static string English(string key) => Base.TryGetValue(key, out var text) ? text : key;

    public static string Caption<TValue>(TValue value) where TValue : struct, Enum =>
        Get($"enum.{typeof(TValue).Name}.{value}");

    public static string ForPlatform(string key) =>
        Get(OperatingSystem.IsWindows() ? key + ".windows" : key + ".mac");

    public static Language Resolve(Language language) => language switch
    {
        Language.System when CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "uk" => Language.Ukrainian,
        Language.System => Language.English,
        _ => language
    };

    public static IReadOnlyDictionary<string, string> Table(Language language)
    {
        var code = Resolve(language) == Language.Ukrainian ? "uk" : "en";
        using var stream = typeof(Strings).Assembly
            .GetManifestResourceStream($"SimplifyMySpace.Core.Localization.{code}.json");
        return stream is null
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize(stream, Json.Default.DictionaryStringString) ?? [];
    }
}
