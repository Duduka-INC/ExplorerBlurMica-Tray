using System.Text.RegularExpressions;

namespace ExplorerBlurMicaTray.Core;

/// <summary>Release versions double as folder names, so only plain dotted numbers are accepted.</summary>
public static partial class EbmVersion
{
    [GeneratedRegex(@"^\d{1,4}(\.\d{1,4}){1,3}$")]
    private static partial Regex VersionPattern();

    public static bool IsValid(string? version) => version is not null && VersionPattern().IsMatch(version);

    /// <summary>True when <paramref name="candidate"/> is newer than <paramref name="current"/>.</summary>
    public static bool IsNewer(string candidate, string? current) =>
        current is null
        || (Version.TryParse(candidate, out var a) && Version.TryParse(current, out var b) && a > b);
}
