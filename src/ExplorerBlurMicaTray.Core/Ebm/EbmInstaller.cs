namespace ExplorerBlurMicaTray.Core;

public enum EbmInstallStep
{
    CheckingRelease,
    Downloading,
    Registering,
}

public sealed record EbmInstallProgress(EbmInstallStep Step, double? Fraction = null);

/// <summary>Installs, updates and removes ExplorerBlurMica. The only part that asks for administrator rights.</summary>
public sealed class EbmInstaller(EbmReleaseClient releases)
{
    public EbmStatus GetStatus() => EbmRegistration.GetStatus();

    /// <summary>The latest release when it is newer than what is installed, otherwise null.</summary>
    public async Task<EbmRelease?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var latest = await releases.GetLatestAsync(ct);
        var status = GetStatus();
        bool installed = status.State is EbmState.Enabled or EbmState.Disabled;
        return installed && !EbmVersion.IsNewer(latest.Version, status.Version) ? null : latest;
    }

    public async Task<EbmStatus> InstallLatestAsync(IProgress<EbmInstallProgress>? progress = null, CancellationToken ct = default)
    {
        progress?.Report(new EbmInstallProgress(EbmInstallStep.CheckingRelease));
        var release = await releases.GetLatestAsync(ct);
        return await InstallAsync(release, progress, ct);
    }

    public async Task<EbmStatus> InstallAsync(EbmRelease release, IProgress<EbmInstallProgress>? progress = null, CancellationToken ct = default)
    {
        string zipPath = Path.Combine(AppPaths.DownloadsDir, release.Version, EbmConstants.ReleaseAssetName);
        progress?.Report(new EbmInstallProgress(EbmInstallStep.Downloading, 0));
        await releases.DownloadAsync(
            release,
            zipPath,
            progress is null ? null : new Progress<double>(f => progress.Report(new EbmInstallProgress(EbmInstallStep.Downloading, f))),
            ct);

        progress?.Report(new EbmInstallProgress(EbmInstallStep.Registering));
        int exitCode = await Elevation.RunAsync(
            ["install", "--zip", zipPath, "--sha256", release.Sha256, "--version", release.Version],
            ct);
        ThrowOnFailure(exitCode);
        return GetStatus();
    }

    /// <summary>Registers the installed ExplorerBlurMica. Asks for administrator rights; Explorer must restart afterwards.</summary>
    public async Task<EbmStatus> EnableAsync(CancellationToken ct = default)
    {
        ThrowOnFailure(await Elevation.RunAsync(["enable"], ct));
        return GetStatus();
    }

    /// <summary>Unregisters ExplorerBlurMica, keeping its files. Asks for administrator rights; Explorer must restart afterwards.</summary>
    public async Task<EbmStatus> DisableAsync(CancellationToken ct = default)
    {
        ThrowOnFailure(await Elevation.RunAsync(["disable"], ct));
        return GetStatus();
    }

    public async Task<EbmStatus> UninstallAsync(CancellationToken ct = default)
    {
        ThrowOnFailure(await Elevation.RunAsync(["uninstall"], ct));
        return GetStatus();
    }

    private static void ThrowOnFailure(int exitCode)
    {
        switch (exitCode)
        {
            case ElevatedCommands.ExitCodes.Ok:
                return;
            case ElevatedCommands.ExitCodes.ChecksumMismatch:
                throw new EbmException(EbmError.ChecksumMismatch, "The downloaded archive did not match its checksum.");
            case ElevatedCommands.ExitCodes.RegistrationFailed:
                throw new EbmException(EbmError.RegistrationFailed, "regsvr32 could not register ExplorerBlurMica.");
            default:
                throw new EbmException(EbmError.Unknown, $"The elevated step failed with exit code {exitCode}.");
        }
    }
}
