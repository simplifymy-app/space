using System.Text.Json.Serialization;
using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core;

public static class AppData
{
    public static readonly string Folder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SimplifyMySpace");

    public static string Path(string name) => System.IO.Path.Combine(Folder, name);
}

public static class Log
{
    private static readonly string File = AppData.Path("log.txt");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}: {exception}");

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(AppData.Folder);
            System.IO.File.AppendAllText(File, $"{DateTime.Now:HH:mm:ss} {level} {message}{Environment.NewLine}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class Json : JsonSerializerContext;
