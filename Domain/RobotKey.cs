using System.Text.RegularExpressions;

namespace KPIAPI.Domain;

public static class RobotKey
{
    private static readonly Regex KeyPattern =
        new(@"^(?<yy>\d{2})(?<nnn>\d{3})-(?<center>[a-z]{2,4})-(?<name>[a-z0-9]+(?:-[a-z0-9]+)*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(string key, out RobotKeyParts parts)
    {
        parts = default;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        key = key.Trim().ToLowerInvariant();

        var match = KeyPattern.Match(key);
        if (!match.Success)
            return false;

        parts = new RobotKeyParts(
            Key: key,
            Year2: match.Groups["yy"].Value,
            Number3: match.Groups["nnn"].Value,
            CenterCode: match.Groups["center"].Value,
            NameSlug: match.Groups["name"].Value,
            DisplayName: SlugToDisplayName(match.Groups["name"].Value)
        );

        return true;
    }

    private static string SlugToDisplayName(string slug)
    {
        var parts = slug.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            parts[i] = p.Length switch
            {
                0 => p,
                1 => p.ToUpperInvariant(),
                _ => char.ToUpperInvariant(p[0]) + p[1..]
            };
        }
        return string.Join(' ', parts);
    }
}

public readonly record struct RobotKeyParts(
    string Key,
    string Year2,
    string Number3,
    string CenterCode,
    string NameSlug,
    string DisplayName
);
