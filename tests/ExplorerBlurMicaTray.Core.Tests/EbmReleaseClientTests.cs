using System.Net;
using System.Security.Cryptography;
using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public class EbmReleaseClientTests
{
    private const string Known210 = "71f678c2356a11a18f9b1e3935d64e035c4f3db71ecf76ff3d27878694f358d5";
    private const string DownloadUrl = "https://github.com/Maplespe/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip";

    private static GitHubRelease Release(string tag = "2.1.0", string? digest = "sha256:" + Known210, string url = DownloadUrl) => new()
    {
        TagName = tag,
        Assets = [new GitHubAsset { Name = "Release_x64.zip", BrowserDownloadUrl = url, Size = 280822, Digest = digest }],
    };

    [Fact]
    public void ParsesKnownRelease()
    {
        var release = EbmReleaseClient.ParseRelease(Release());

        Assert.Equal("2.1.0", release.Version);
        Assert.Equal(Known210, release.Sha256);
        Assert.Equal(new Uri(DownloadUrl), release.DownloadUrl);
    }

    [Fact]
    public void KnownHashIsUsedWhenGitHubHasNoDigest()
    {
        Assert.Equal(Known210, EbmReleaseClient.ParseRelease(Release(digest: null)).Sha256);
    }

    [Fact]
    public void DigestThatContradictsKnownHashIsRejected()
    {
        var e = Assert.Throws<EbmException>(() => EbmReleaseClient.ParseRelease(Release(digest: "sha256:" + new string('0', 64))));
        Assert.Equal(EbmError.UntrustedRelease, e.Error);
    }

    [Fact]
    public void UnknownVersionWithoutDigestIsRejected()
    {
        var e = Assert.Throws<EbmException>(() => EbmReleaseClient.ParseRelease(Release(tag: "9.9.9", digest: null,
            url: "https://github.com/Maplespe/ExplorerBlurMica/releases/download/9.9.9/Release_x64.zip")));
        Assert.Equal(EbmError.UntrustedRelease, e.Error);
    }

    [Theory]
    [InlineData("http://github.com/Maplespe/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip")]
    [InlineData("https://example.com/Maplespe/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip")]
    [InlineData("https://github.com/someone/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip")]
    public void ForeignDownloadUrlIsRejected(string url)
    {
        Assert.Throws<EbmException>(() => EbmReleaseClient.ParseRelease(Release(url: url)));
    }

    [Theory]
    [InlineData("2.1.0/../x")]
    [InlineData("latest")]
    public void TagThatIsNotAPlainVersionIsRejected(string tag)
    {
        Assert.Throws<EbmException>(() => EbmReleaseClient.ParseRelease(Release(tag: tag)));
    }

    [Fact]
    public void ReleaseWithoutArchiveIsRejected()
    {
        var e = Assert.Throws<EbmException>(() => EbmReleaseClient.ParseRelease(new GitHubRelease { TagName = "2.1.0", Assets = [] }));
        Assert.Equal(EbmError.ReleaseNotFound, e.Error);
    }

    [Fact]
    public async Task DownloadKeepsFileWhenChecksumMatches()
    {
        byte[] payload = [1, 2, 3, 4];
        var release = new EbmRelease("1.0", new Uri(DownloadUrl), Convert.ToHexStringLower(SHA256.HashData(payload)), payload.Length);
        string path = Path.Combine(Path.GetTempPath(), $"ebmt-{Guid.NewGuid():N}", "a.zip");
        try
        {
            using var http = new HttpClient(new FixedResponse(payload));
            await new EbmReleaseClient(http).DownloadAsync(release, path);

            Assert.Equal(payload, File.ReadAllBytes(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadLeavesNothingWhenChecksumDiffers()
    {
        var release = new EbmRelease("1.0", new Uri(DownloadUrl), new string('0', 64), 4);
        string dir = Path.Combine(Path.GetTempPath(), $"ebmt-{Guid.NewGuid():N}");
        try
        {
            using var http = new HttpClient(new FixedResponse([1, 2, 3, 4]));
            var e = await Assert.ThrowsAsync<EbmException>(() => new EbmReleaseClient(http).DownloadAsync(release, Path.Combine(dir, "a.zip")));

            Assert.Equal(EbmError.ChecksumMismatch, e.Error);
            Assert.Empty(Directory.EnumerateFiles(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class FixedResponse(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
    }
}
