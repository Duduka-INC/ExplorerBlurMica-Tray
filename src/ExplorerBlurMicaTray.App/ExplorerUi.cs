namespace ExplorerBlurMicaTray.App;

/// <summary>Which parts of the File Explorer window this Windows version has.</summary>
internal static class ExplorerUi
{
    private static int Build => Environment.OSVersion.Version.Build;

    /// <summary>
    /// The Win32 address bar that clearAddress clears. Windows 11 23H2 moved the address bar and
    /// search into the WinUI top bar, where clearWinUIBg covers them.
    /// </summary>
    public static bool HasClassicAddressBar => Build < 22631;

    /// <summary>The WinUI (or XAML Islands) toolbar that clearWinUIBg clears, Windows 11 only.</summary>
    public static bool HasWinUIToolbar => Build >= 22000;
}
