using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExplorerBlurMicaTray.Core;

/// <summary>An official ExplorerBlurMica release archive and the hash it must match.</summary>
public sealed record EbmRelease(string Version, Uri DownloadUrl, string Sha256, long Size);

/// <summary>Finds and downloads official ExplorerBlurMica releases from GitHub.</summary>
public sealed class EbmReleaseClient(HttpClient http)
{
    public static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(AppPaths.AppFolderName);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public async Task<EbmRelease> GetLatestAsync(CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{EbmConstants.GitHubOwner}/{EbmConstants.GitHubRepo}/releases/latest";
        await using var stream = await http.GetStreamAsync(url, ct);
        var release = await JsonSerializer.DeserializeAsync(stream, GitHubJsonContext.Default.GitHubRelease, ct)
            ?? throw new EbmException(EbmError.ReleaseNotFound, "GitHub returned an empty release.");
        return ParseRelease(release);
    }

    /// <summary>Downloads the archive and keeps it only if its SHA-256 matches the release.</summary>
    public async Task DownloadAsync(EbmRelease release, string destinationPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        long total = response.Content.Headers.ContentLength ?? release.Size;

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        string partPath = destinationPath + ".part";
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        try
        {
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var target = File.Create(partPath))
            {
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    received += read;
                    if (received > EbmConstants.MaxArchiveBytes)
                        throw new EbmException(EbmError.UntrustedRelease, "The release archive is larger than expected.");
                    hash.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    if (total > 0)
                        progress?.Report(Math.Min(1.0, (double)received / total));
                }
            }

            string actual = Convert.ToHexStringLower(hash.GetHashAndReset());
            if (actual != release.Sha256)
                throw new EbmException(EbmError.ChecksumMismatch, $"Checksum mismatch for {release.Version}: expected {release.Sha256}, got {actual}.");

            File.Move(partPath, destinationPath, overwrite: true);
        }
        finally
        {
            File.Delete(partPath);
        }
    }

    internal static EbmRelease ParseRelease(GitHubRelease release)
    {
        var asset = release.Assets?.FirstOrDefault(a => a.Name == EbmConstants.ReleaseAssetName)
            ?? throw new EbmException(EbmError.ReleaseNotFound, $"Release {release.TagName} has no {EbmConstants.ReleaseAssetName}.");

        string version = release.TagName.TrimStart('v', 'V');
        if (!EbmVersion.IsValid(version))
            throw new EbmException(EbmError.UntrustedRelease, $"Unexpected release tag '{release.TagName}'.");

        if (!Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var url)
            || url.Scheme != Uri.UriSchemeHttps
            || !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || !url.AbsolutePath.StartsWith($"/{EbmConstants.GitHubOwner}/{EbmConstants.GitHubRepo}/releases/download/", StringComparison.OrdinalIgnoreCase))
            throw new EbmException(EbmError.UntrustedRelease, $"Unexpected download URL '{asset.BrowserDownloadUrl}'.");

        string? sha256 = null;
        if (asset.Digest is { } digest && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            sha256 = digest["sha256:".Length..].ToLowerInvariant();

        if (EbmConstants.KnownArchiveHashes.TryGetValue(version, out var known))
        {
            if (sha256 is not null && sha256 != known)
                throw new EbmException(EbmError.UntrustedRelease, $"GitHub reports a different checksum for {version} than the known release.");
            sha256 = known;
        }

        if (sha256 is null || !IsSha256Hex(sha256))
            throw new EbmException(EbmError.UntrustedRelease, $"No checksum is available for {version}; refusing to install it.");

        return new EbmRelease(version, url, sha256, asset.Size);
    }

    internal static bool IsSha256Hex(string value) => value.Length == 64 && value.All(char.IsAsciiHexDigitLower);
}

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
    [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
}

internal sealed class GitHubAsset
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("digest")] public string? Digest { get; set; }
}

[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class GitHubJsonContext : JsonSerializerContext;
