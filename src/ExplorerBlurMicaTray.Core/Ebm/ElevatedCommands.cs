using System.Security.AccessControl;
using System.Security.Principal;

namespace ExplorerBlurMicaTray.Core;

/// <summary>
/// The commands that run with administrator rights. Every executable that uses
/// <see cref="Elevation"/> must call <see cref="TryRun"/> first thing in Main.
/// </summary>
public static class ElevatedCommands
{
    public const string Flag = "--ebmt-elevated";

    public static class ExitCodes
    {
        public const int Ok = 0;
        public const int Failed = 1;
        public const int BadArguments = 2;
        public const int ChecksumMismatch = 3;
        public const int RegistrationFailed = 4;
    }

    /// <summary>Runs an elevated command when the process was started for one.</summary>
    public static bool TryRun(string[] args, out int exitCode)
    {
        if (args.Length == 0 || args[0] != Flag)
        {
            exitCode = 0;
            return false;
        }
        exitCode = Run(args[1..]);
        return true;
    }

    internal static int Run(IReadOnlyList<string> args)
    {
        try
        {
            return args.Count == 0 ? ExitCodes.BadArguments : args[0] switch
            {
                "install" => InstallArgs.TryParse(args, out var install) ? Install(install) : ExitCodes.BadArguments,
                "uninstall" => Uninstall(),
                _ => ExitCodes.BadArguments,
            };
        }
        catch (EbmException e) when (e.Error == EbmError.ChecksumMismatch)
        {
            return ExitCodes.ChecksumMismatch;
        }
        catch (Exception)
        {
            return ExitCodes.Failed;
        }
    }

    internal sealed record InstallArgs(string ZipPath, string Sha256, string Version, string UserSid)
    {
        public static bool TryParse(IReadOnlyList<string> args, out InstallArgs result)
        {
            result = null!;
            string? zip = null, sha = null, version = null, sid = null;
            for (int i = 1; i + 1 < args.Count; i += 2)
            {
                switch (args[i])
                {
                    case "--zip": zip = args[i + 1]; break;
                    case "--sha256": sha = args[i + 1]; break;
                    case "--version": version = args[i + 1]; break;
                    case "--user-sid": sid = args[i + 1]; break;
                    default: return false;
                }
            }

            if (zip is null || !Path.IsPathFullyQualified(zip)
                || sha is null || !EbmReleaseClient.IsSha256Hex(sha)
                || !EbmVersion.IsValid(version)
                || sid is null || !IsSid(sid))
                return false;

            result = new InstallArgs(zip, sha, version!, sid);
            return true;
        }

        private static bool IsSid(string value)
        {
            try
            {
                _ = new SecurityIdentifier(value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    private static int Install(InstallArgs args)
    {
        if (EbmConstants.KnownArchiveHashes.TryGetValue(args.Version, out var known) && known != args.Sha256)
            return ExitCodes.ChecksumMismatch;

        byte[] zip = EbmPackage.ReadVerified(args.ZipPath, args.Sha256);
        string targetDir = AppPaths.EbmVersionDir(args.Version);
        string? previousDll = EbmRegistration.GetRegisteredDllPath();
        string? previousDir = previousDll is null ? null : Path.GetDirectoryName(previousDll);
        bool upgrading = previousDir is not null && !PathsEqual(previousDir, targetDir);

        EbmPackage.Extract(zip, targetDir);

        // Keep the user's settings when moving to a new version.
        string targetConfig = Path.Combine(targetDir, EbmConstants.ConfigFileName);
        if (upgrading && File.Exists(Path.Combine(previousDir!, EbmConstants.ConfigFileName)))
            File.Copy(Path.Combine(previousDir!, EbmConstants.ConfigFileName), targetConfig, overwrite: true);

        // The app runs without elevation and writes the config in place.
        GrantModify(targetConfig, new SecurityIdentifier(args.UserSid));

        if (upgrading)
            EbmRegistration.RunRegsvr32(previousDll!, unregister: true);

        if (EbmRegistration.RunRegsvr32(Path.Combine(targetDir, EbmConstants.DllFileName), unregister: false) != 0)
            return ExitCodes.RegistrationFailed;

        DeleteOtherVersions(keep: targetDir);
        return ExitCodes.Ok;
    }

    private static int Uninstall()
    {
        string? dll = EbmRegistration.GetRegisteredDllPath();
        if (dll is null)
            return ExitCodes.Ok;

        if (File.Exists(dll) && EbmRegistration.RunRegsvr32(dll, unregister: true) != 0)
            return ExitCodes.RegistrationFailed;

        DeleteOtherVersions(keep: null);
        return ExitCodes.Ok;
    }

    private static void GrantModify(string file, SecurityIdentifier user)
    {
        var info = new FileInfo(file);
        var security = info.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Modify, AccessControlType.Allow));
        info.SetAccessControl(security);
    }

    /// <summary>
    /// Removes version folders the app installed earlier. Explorer keeps a loaded DLL locked until it
    /// restarts, so a folder that cannot be deleted now is left for a later install or uninstall.
    /// </summary>
    private static void DeleteOtherVersions(string? keep)
    {
        if (!Directory.Exists(AppPaths.EbmRoot))
            return;
        foreach (string dir in Directory.EnumerateDirectories(AppPaths.EbmRoot))
        {
            if (keep is not null && PathsEqual(dir, keep))
                continue;
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
}
