namespace ExplorerBlurMicaTray.Core;

/// <summary>Facts about the upstream ExplorerBlurMica project that the app depends on.</summary>
public static class EbmConstants
{
    /// <summary>COM class ExplorerBlurMica registers as an Explorer folder extension.</summary>
    public const string Clsid = "{887D3A6A-502E-4AF5-9CE6-D515E12AFE89}";

    public const string GitHubOwner = "Maplespe";
    public const string GitHubRepo = "ExplorerBlurMica";
    public const string ReleaseAssetName = "Release_x64.zip";

    public const string DllEntryName = "Release/ExplorerBlurMica.dll";
    public const string ConfigEntryName = "Release/config.ini";
    public const string DllFileName = "ExplorerBlurMica.dll";
    public const string ConfigFileName = "config.ini";

    /// <summary>
    /// Effect written into a freshly installed config. The shipped default is Mica, but Mica draws
    /// nothing on recent Windows 11 builds (seen on 26200.9711; upstream issue #299) while Acrylic works.
    /// </summary>
    public const EbmEffect InstallDefaultEffect = EbmEffect.Acrylic;

    /// <summary>Upper bound for the release archive; the real one is under 300 KB.</summary>
    public const long MaxArchiveBytes = 20 * 1024 * 1024;

    /// <summary>
    /// SHA-256 of official release archives, checked by hand. Used when the GitHub API
    /// returns no digest, and to catch a digest that does not match a known release.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> KnownArchiveHashes = new Dictionary<string, string>
    {
        ["2.1.0"] = "71f678c2356a11a18f9b1e3935d64e035c4f3db71ecf76ff3d27878694f358d5",
    };
}
