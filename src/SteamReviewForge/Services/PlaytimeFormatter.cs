using System.Globalization;

namespace SteamReviewForge.Services;

public static class PlaytimeFormatter
{
    public const decimal MaximumHours = 999999999m;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = RemoveLegacySuffix(value.Trim());

        if (!normalized.Contains('.') &&
            normalized.Count(character => character == ',') == 1)
        {
            normalized = normalized.Replace(',', '.');
        }

        if (!decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var hours))
        {
            return string.Empty;
        }

        hours = Math.Clamp(hours, 0, MaximumHours);
        hours = decimal.Truncate(hours * 10) / 10;

        return hours.ToString(
            "0.#",
            CultureInfo.InvariantCulture);
    }

    private static string RemoveLegacySuffix(string value)
    {
        string[] legacySuffixes =
        [
            " hours",
            " hour",
            " hrs",
            " hr"
        ];

        foreach (var suffix in legacySuffixes)
        {
            if (value.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return value[..^suffix.Length].TrimEnd();
            }
        }

        return value;
    }
}
