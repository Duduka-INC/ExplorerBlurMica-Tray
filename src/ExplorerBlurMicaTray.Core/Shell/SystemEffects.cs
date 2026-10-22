using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ExplorerBlurMicaTray.Core;

/// <summary>Windows settings that turn Mica and Acrylic off for every app.</summary>
public static partial class SystemEffects
{
    /// <summary>Settings → Personalization → Colors → Transparency effects.</summary>
    public static bool IsTransparencyEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("EnableTransparency") is not int value || value != 0;
    }

    /// <summary>Settings → Personalization → Colors → app mode; Explorer and ExplorerBlurMica's [light]/[dark] follow it.</summary>
    public static bool IsAppsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value != 0;
    }

    /// <summary>Energy saver (battery saver) also disables transparency while it is on.</summary>
    public static bool IsEnergySaverOn() => GetSystemPowerStatus(out var status) && status.SystemStatusFlag == 1;

    public static Uri TransparencySettingsUri { get; } = new("ms-settings:colors");

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemPowerStatus(out SystemPowerStatus status);
}
