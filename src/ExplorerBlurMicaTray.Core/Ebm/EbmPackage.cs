using System.IO.Compression;
using System.Security.Cryptography;

namespace ExplorerBlurMicaTray.Core;

/// <summary>Reads the official release archive.</summary>
public static class EbmPackage
{
    /// <summary>Reads the archive into memory and checks it, so the checked bytes are the ones extracted.</summary>
    public static byte[] ReadVerified(string zipPath, string expectedSha256)
    {
        var info = new FileInfo(zipPath);
        if (!info.Exists)
            throw new EbmException(EbmError.ReleaseNotFound, $"Archive '{zipPath}' does not exist.");
        if (info.Length > EbmConstants.MaxArchiveBytes)
            throw new EbmException(EbmError.UntrustedRelease, "The release archive is larger than expected.");

        byte[] bytes = File.ReadAllBytes(zipPath);
        string actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (actual != expectedSha256)
            throw new EbmException(EbmError.ChecksumMismatch, $"Checksum mismatch: expected {expectedSha256}, got {actual}.");
        return bytes;
    }

    /// <summary>
    /// Writes the DLL and, unless one is already there, the default config into <paramref name="targetDir"/>,
    /// switching a new config to <see cref="EbmConstants.InstallDefaultEffect"/>.
    /// Only these two entries are read, so nothing else in the archive can land on disk.
    /// </summary>
    public static void Extract(byte[] zipBytes, string targetDir)
    {
        using var archive = new ZipArchive(new MemoryStream(zipBytes, writable: false), ZipArchiveMode.Read);
        var dll = archive.GetEntry(EbmConstants.DllEntryName)
            ?? throw new EbmException(EbmError.UntrustedRelease, $"The archive has no {EbmConstants.DllEntryName}.");
        var config = archive.GetEntry(EbmConstants.ConfigEntryName);

        Directory.CreateDirectory(targetDir);
        string dllPath = Path.Combine(targetDir, EbmConstants.DllFileName);
        byte[] dllBytes = ReadEntry(dll);
        // Explorer locks a loaded DLL; reinstalling the same version must not try to overwrite it.
        if (!File.Exists(dllPath) || !File.ReadAllBytes(dllPath).AsSpan().SequenceEqual(dllBytes))
            File.WriteAllBytes(dllPath, dllBytes);

        string configPath = Path.Combine(targetDir, EbmConstants.ConfigFileName);
        if (config is not null && !File.Exists(configPath))
        {
            File.WriteAllBytes(configPath, ReadEntry(config));
            var defaults = EbmConfig.Load(configPath);
            defaults.Effect = EbmConstants.InstallDefaultEffect;
            defaults.Save(configPath);
        }
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        if (entry.Length > EbmConstants.MaxArchiveBytes)
            throw new EbmException(EbmError.UntrustedRelease, $"{entry.FullName} is larger than expected.");
        using var stream = entry.Open();
        using var buffer = new MemoryStream((int)entry.Length);
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
