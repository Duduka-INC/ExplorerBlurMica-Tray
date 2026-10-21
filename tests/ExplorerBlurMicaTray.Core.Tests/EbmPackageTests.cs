using System.IO.Compression;
using System.Security.Cryptography;
using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.Core.Tests;

public sealed class EbmPackageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ebmt-pkg-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
        }
        return buffer.ToArray();
    }

    [Fact]
    public void ExtractsOnlyDllAndConfig()
    {
        byte[] zip = Zip(
            ("ReadMe.txt", "readme"),
            ("Release/ExplorerBlurMica.dll", "dll"),
            ("Release/config.ini", "[config]"),
            ("Release/register.cmd", "regsvr32"),
            ("../evil.dll", "evil"));

        EbmPackage.Extract(zip, _dir);

        Assert.Equal(["ExplorerBlurMica.dll", "config.ini"], Directory.EnumerateFiles(_dir).Select(Path.GetFileName).Order().Reverse());
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_dir)!, "evil.dll")));
    }

    [Fact]
    public void NewConfigGetsInstallDefaultEffect()
    {
        EbmPackage.Extract(Zip(("Release/ExplorerBlurMica.dll", "dll"), ("Release/config.ini", "[config]\r\neffect=2\r\nshowLine=true\r\n")), _dir);

        var config = EbmConfig.Load(Path.Combine(_dir, "config.ini"));
        Assert.Equal(EbmConstants.InstallDefaultEffect, config.Effect);
        Assert.True(config.ShowLine);
    }

    [Fact]
    public void KeepsExistingConfig()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.ini"), "mine");

        EbmPackage.Extract(Zip(("Release/ExplorerBlurMica.dll", "dll"), ("Release/config.ini", "default")), _dir);

        Assert.Equal("mine", File.ReadAllText(Path.Combine(_dir, "config.ini")));
    }

    [Fact]
    public void SameDllIsNotRewritten()
    {
        byte[] zip = Zip(("Release/ExplorerBlurMica.dll", "dll"));
        EbmPackage.Extract(zip, _dir);
        string dll = Path.Combine(_dir, "ExplorerBlurMica.dll");

        // An open handle without write sharing stands in for Explorer holding the DLL.
        using (File.Open(dll, FileMode.Open, FileAccess.Read, FileShare.Read))
            EbmPackage.Extract(zip, _dir);
    }

    [Fact]
    public void ArchiveWithoutDllIsRejected()
    {
        Assert.Throws<EbmException>(() => EbmPackage.Extract(Zip(("Release/config.ini", "x")), _dir));
    }

    [Fact]
    public void ReadVerifiedRejectsWrongChecksum()
    {
        Directory.CreateDirectory(_dir);
        string path = Path.Combine(_dir, "a.zip");
        File.WriteAllBytes(path, [1, 2, 3]);

        Assert.Equal([1, 2, 3], EbmPackage.ReadVerified(path, Convert.ToHexStringLower(SHA256.HashData(new byte[] { 1, 2, 3 }))));
        var e = Assert.Throws<EbmException>(() => EbmPackage.ReadVerified(path, new string('0', 64)));
        Assert.Equal(EbmError.ChecksumMismatch, e.Error);
    }
}
