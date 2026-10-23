using Microsoft.Win32;

namespace ExplorerBlurMicaTray.Core;

/// <summary>
/// Starting the app with Windows, per user: a value in the user's Run key, which needs no
/// administrator rights. Windows' own Startup apps page can switch it off without deleting it,
/// by marking it in StartupApproved; that counts as off here too.
/// </summary>
public sealed class Autostart(RegistryKey userRoot)
{
    /// <summary>Also written by Setup; must match the value name in the installer script.</summary>
    public const string ValueName = "ExplorerBlurMica Tray";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static Autostart ForCurrentUser { get; } = new(Registry.CurrentUser);

    public bool IsEnabled
    {
        get
        {
            using var run = userRoot.OpenSubKey(RunKey);
            if (run?.GetValue(ValueName) is not string { Length: > 0 })
                return false;
            using var approved = userRoot.OpenSubKey(ApprovedKey);
            // The first byte is 2 for allowed and 3 (odd) for switched off in Startup apps.
            return approved?.GetValue(ValueName) is not byte[] { Length: > 0 } state || (state[0] & 1) == 0;
        }
    }

    public void Enable(string exePath)
    {
        using (var run = userRoot.CreateSubKey(RunKey))
            run.SetValue(ValueName, $"\"{exePath}\"");
        // Clear a "switched off" mark left by Startup apps, or the entry would stay off.
        using var approved = userRoot.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public void Disable()
    {
        using (var run = userRoot.OpenSubKey(RunKey, writable: true))
            run?.DeleteValue(ValueName, throwOnMissingValue: false);
        using var approved = userRoot.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
