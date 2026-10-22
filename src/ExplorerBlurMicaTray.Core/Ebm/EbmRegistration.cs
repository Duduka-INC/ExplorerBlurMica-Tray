using System.Diagnostics;
using Microsoft.Win32;

namespace ExplorerBlurMicaTray.Core;

public enum EbmState
{
    NotInstalled,
    /// <summary>Installed and registered: Explorer loads it.</summary>
    Enabled,
    /// <summary>Installed but not registered: Explorer looks as usual (the app's "Default").</summary>
    Disabled,
    /// <summary>Registered, but the DLL it points to is gone.</summary>
    Broken,
}

public sealed record EbmStatus(EbmState State, string? DllPath, string? Version)
{
    public string? ConfigPath =>
        DllPath is null ? null : Path.Combine(Path.GetDirectoryName(DllPath)!, EbmConstants.ConfigFileName);
}

/// <summary>Reads and changes how ExplorerBlurMica is registered with Windows.</summary>
public static class EbmRegistration
{
    public static string? GetRegisteredDllPath()
    {
        using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{EbmConstants.Clsid}\InprocServer32");
        return key?.GetValue(null) as string is { Length: > 0 } path
            ? Environment.ExpandEnvironmentVariables(path)
            : null;
    }

    public static EbmStatus GetStatus()
    {
        string? registered = GetRegisteredDllPath();
        if (registered is not null)
        {
            return File.Exists(registered)
                ? new EbmStatus(EbmState.Enabled, registered, GetVersion(registered))
                : new EbmStatus(EbmState.Broken, registered, null);
        }

        string? installed = FindInstalledDll();
        return installed is null
            ? new EbmStatus(EbmState.NotInstalled, null, null)
            : new EbmStatus(EbmState.Disabled, installed, GetVersion(installed));
    }

    /// <summary>The newest version the app installed under <see cref="AppPaths.EbmRoot"/>, registered or not.</summary>
    internal static string? FindInstalledDll() => FindInstalledDll(AppPaths.EbmRoot);

    internal static string? FindInstalledDll(string ebmRoot)
    {
        if (!Directory.Exists(ebmRoot))
            return null;
        return Directory.EnumerateDirectories(ebmRoot)
            .Select(dir => (Dir: dir, Name: Path.GetFileName(dir)))
            .Where(d => EbmVersion.IsValid(d.Name) && File.Exists(Path.Combine(d.Dir, EbmConstants.DllFileName)))
            .OrderByDescending(d => Version.Parse(d.Name))
            .Select(d => Path.Combine(d.Dir, EbmConstants.DllFileName))
            .FirstOrDefault();
    }

    /// <summary>The app installs each version into a folder named after it; other installs fall back to the file version.</summary>
    internal static string? GetVersion(string dllPath)
    {
        string folder = Path.GetFileName(Path.GetDirectoryName(dllPath)!);
        if (EbmVersion.IsValid(folder))
            return folder;
        return FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
    }

    /// <summary>Runs regsvr32 silently. Needs an elevated process.</summary>
    internal static int RunRegsvr32(string dllPath, bool unregister)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "regsvr32.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("/s");
        if (unregister)
            psi.ArgumentList.Add("/u");
        psi.ArgumentList.Add(dllPath);

        using var process = Process.Start(psi)!;
        process.WaitForExit();
        return process.ExitCode;
    }
}
