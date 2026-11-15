using System.Globalization;
using SimplifyMySpace.Core.Settings;

namespace SimplifyMySpace.Core.Text;

public static class Format
{
    private static readonly string[] DecimalNames = ["B", "KB", "MB", "GB", "TB", "PB"];

    private static readonly string[] BinaryNames = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];

    public static string Bytes(long bytes, SizeUnits units)
    {
        var step = units == SizeUnits.Binary ? 1024d : 1000d;
        var names = units == SizeUnits.Binary ? BinaryNames : DecimalNames;
        var value = (double)Math.Abs(bytes);
        var index = 0;
        while (value >= step && index < names.Length - 1)
        {
            value /= step;
            index++;
        }

        var sign = bytes < 0 ? "-" : string.Empty;
        var number = index == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString(value >= 100 ? "0.#" : "0.##", CultureInfo.InvariantCulture);
        return $"{sign}{number} {names[index]}";
    }

    public static string Percent(double share) => share.ToString("P0", CultureInfo.InvariantCulture);

    public static string Count(long count) => count.ToString("N0", CultureInfo.InvariantCulture);

    public static string Date(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.CurrentCulture);

    public static string Elapsed(TimeSpan elapsed) => elapsed.TotalHours >= 1
        ? elapsed.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
        : elapsed.ToString(@"m\:ss", CultureInfo.InvariantCulture);

    public static string MiddleTruncate(string text, int maxLength)
    {
        if (text.Length <= maxLength || maxLength < 5)
        {
            return text;
        }

        var head = (maxLength - 1) / 2;
        var tail = maxLength - 1 - head;
        return string.Concat(text.AsSpan(0, head), "…", text.AsSpan(text.Length - tail));
    }
}
